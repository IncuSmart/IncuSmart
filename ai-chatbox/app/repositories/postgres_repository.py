from __future__ import annotations

from dataclasses import dataclass
from typing import Any

import pandas as pd
from sqlalchemy import create_engine, text
from sqlalchemy.engine import Engine

from app.config import Settings


@dataclass
class TemplateConfigRow:
    template_id: str
    template_name: str
    egg_type: str | None
    batch_index: int
    phase_name: str | None
    number_of_days: int
    config_id: str
    config_code: str
    config_name: str
    config_type: str | None
    config_unit: str | None
    target_value: float | None
    min_value: float | None
    max_value: float | None


class PostgresRepository:
    def __init__(self, settings: Settings):
        self._engine: Engine = create_engine(settings.postgres_dsn, pool_pre_ping=True)

    def fetch_training_dataset(self) -> pd.DataFrame:
        query = text(
            """
            WITH season_base AS (
                SELECT
                    hs.id AS season_id,
                    hs.incubator_id,
                    lower(coalesce(hs.egg_type, 'unknown')) AS egg_type,
                    coalesce(hs.total_eggs, 0) AS total_eggs,
                    coalesce(hs.success_count, 0) AS success_count,
                    coalesce(hs.fail_count, 0) AS fail_count
                FROM hatching_seasons hs
                WHERE hs.deleted_at IS NULL
                  AND coalesce(hs.total_eggs, 0) > 0
                  AND coalesce(hs.success_count, 0) + coalesce(hs.fail_count, 0) > 0
            ),
            sensor_summary AS (
                SELECT
                    hs.id AS season_id,
                    AVG(sr.value) FILTER (
                        WHERE lower(coalesce(c.type, c.code, '')) LIKE '%temp%'
                           OR lower(coalesce(c.name, '')) LIKE '%temperature%'
                    ) AS ambient_temperature,
                    AVG(sr.value) FILTER (
                        WHERE lower(coalesce(c.type, c.code, '')) LIKE '%humid%'
                           OR lower(coalesce(c.name, '')) LIKE '%humidity%'
                    ) AS ambient_humidity
                FROM hatching_seasons hs
                JOIN incubator_config_instances ici ON ici.incubator_id = hs.incubator_id AND ici.deleted_at IS NULL
                JOIN sensors s ON s.config_instance_id = ici.id AND s.deleted_at IS NULL
                JOIN sensor_readings sr ON sr.sensor_id = s.id AND sr.deleted_at IS NULL
                JOIN configs c ON c.id = ici.config_id AND c.deleted_at IS NULL
                WHERE hs.deleted_at IS NULL
                  AND sr.recorded_at >= hs.start_date
                  AND sr.recorded_at < coalesce(hs.end_date, CURRENT_DATE) + INTERVAL '1 day'
                GROUP BY hs.id
            ),
            batch_cfg AS (
                SELECT
                    hb.season_id,
                    hb.batch_index,
                    hb.day_start,
                    hb.day_end,
                    c.code AS config_code,
                    c.name AS config_name,
                    c.type AS config_type,
                    c.unit AS config_unit,
                    hbc.target_value,
                    hbc.min_value,
                    hbc.max_value
                FROM hatching_batches hb
                JOIN hatching_batch_configs hbc ON hbc.batch_id = hb.id AND hbc.deleted_at IS NULL
                JOIN configs c ON c.id = hbc.config_id AND c.deleted_at IS NULL
                WHERE hb.deleted_at IS NULL
            )
            SELECT
                sb.season_id,
                sb.incubator_id,
                sb.egg_type,
                sb.total_eggs,
                sb.success_count,
                sb.fail_count,
                ss.ambient_temperature,
                ss.ambient_humidity,
                bc.batch_index,
                bc.day_start,
                bc.day_end,
                bc.config_code,
                bc.config_name,
                bc.config_type,
                bc.config_unit,
                bc.target_value,
                bc.min_value,
                bc.max_value
            FROM season_base sb
            LEFT JOIN sensor_summary ss ON ss.season_id = sb.season_id
            LEFT JOIN batch_cfg bc ON bc.season_id = sb.season_id
            """
        )
        with self._engine.begin() as connection:
            return pd.read_sql_query(query, connection)

    def fetch_cloud_training_rows(self) -> list[dict[str, Any]]:
        frame = self.fetch_training_dataset()
        if frame.empty:
            return []

        rows: list[dict[str, Any]] = []
        for season_id, group in frame.groupby("season_id", dropna=False):
            first = group.iloc[0]
            total_eggs = int(first.get("total_eggs") or 0)
            success_count = int(first.get("success_count") or 0)
            if total_eggs <= 0:
                continue
            rows.append(
                {
                    "season_id": str(season_id),
                    "egg_type": str(first.get("egg_type") or "unknown"),
                    "total_eggs": total_eggs,
                    "ambient_temperature": _as_optional_float(first.get("ambient_temperature")),
                    "ambient_humidity": _as_optional_float(first.get("ambient_humidity")),
                    "success_rate": min(1.0, max(0.0, success_count / total_eggs)),
                }
            )
        return rows

    def fetch_template_configs(self) -> list[TemplateConfigRow]:
        query = text(
            """
            SELECT
                hst.id AS template_id,
                hst.name AS template_name,
                lower(coalesce(hst.egg_type, 'unknown')) AS egg_type,
                hstb.batch_index,
                hstb.name AS phase_name,
                hstb.number_of_days,
                c.id AS config_id,
                c.code AS config_code,
                c.name AS config_name,
                c.type AS config_type,
                c.unit AS config_unit,
                hstbc.target_value,
                hstbc.min_value,
                hstbc.max_value
            FROM hatching_season_templates hst
            JOIN hatching_season_template_batches hstb
                ON hstb.template_id = hst.id AND hstb.deleted_at IS NULL
            JOIN hatching_season_template_batch_configs hstbc
                ON hstbc.template_batch_id = hstb.id AND hstbc.deleted_at IS NULL
            JOIN configs c
                ON c.id = hstbc.config_id AND c.deleted_at IS NULL
            WHERE hst.deleted_at IS NULL
            ORDER BY hst.name, hstb.batch_index, c.code
            """
        )
        with self._engine.begin() as connection:
            frame = pd.read_sql_query(query, connection)
        return [TemplateConfigRow(**record) for record in frame.to_dict(orient="records")]

    def fetch_sensor_summary(self) -> pd.DataFrame:
        query = text(
            """
            SELECT
                i.id AS incubator_id,
                lower(coalesce(c.type, c.code, 'unknown')) AS config_key,
                AVG(sr.value) AS avg_value,
                MIN(sr.value) AS min_value,
                MAX(sr.value) AS max_value
            FROM sensor_readings sr
            JOIN sensors s ON s.id = sr.sensor_id AND s.deleted_at IS NULL
            JOIN incubator_config_instances ici ON ici.id = s.config_instance_id AND ici.deleted_at IS NULL
            JOIN incubators i ON i.id = ici.incubator_id AND i.deleted_at IS NULL
            JOIN configs c ON c.id = ici.config_id AND c.deleted_at IS NULL
            WHERE sr.deleted_at IS NULL
            GROUP BY i.id, lower(coalesce(c.type, c.code, 'unknown'))
            """
        )
        with self._engine.begin() as connection:
            return pd.read_sql_query(query, connection)

    def fetch_incubator_config_limits(self, incubator_id: str) -> dict[str, tuple[float | None, float | None]]:
        query = text(
            """
            SELECT
                lower(c.code) AS config_code,
                MAX(ici.min_value) AS min_value,
                MIN(ici.max_value) AS max_value
            FROM incubator_config_instances ici
            JOIN configs c ON c.id = ici.config_id AND c.deleted_at IS NULL
            WHERE ici.incubator_id = :incubator_id
              AND ici.deleted_at IS NULL
            GROUP BY lower(c.code)
            """
        )
        with self._engine.begin() as connection:
            rows = connection.execute(query, {"incubator_id": incubator_id}).mappings().all()
        return {
            str(row["config_code"]): (_as_optional_float(row["min_value"]), _as_optional_float(row["max_value"]))
            for row in rows
        }


def _as_optional_float(value: Any) -> float | None:
    return None if value is None else float(value)
