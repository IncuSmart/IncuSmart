from __future__ import annotations

import math


EXPECTED_FINAL_DAY = {
    "chicken": 21,
    "duck": 28,
    "quail": 18,
    "goose": 30,
}


def validate_synthetic_record(record: object) -> list[str]:
    if not isinstance(record, dict):
        return ["record_not_object"]

    issues: list[str] = []
    egg_type = str(record.get("egg_type") or "").lower()
    if egg_type not in EXPECTED_FINAL_DAY:
        issues.append("invalid_egg_type")

    total_eggs = _as_float(record.get("total_eggs"))
    if total_eggs is None or total_eggs <= 0:
        issues.append("invalid_total_eggs")

    success_rate = _as_float(record.get("expected_success_rate"))
    if success_rate is None or not 0 <= success_rate <= 1:
        issues.append("invalid_expected_success_rate")

    phases = record.get("phases")
    if not isinstance(phases, list) or len(phases) != 3:
        issues.append("invalid_phase_count")
        return issues

    phase_indexes: set[int] = set()
    final_day = 0
    humidity_targets: list[tuple[int, float]] = []
    turning_targets: list[tuple[int, float]] = []
    phase_days: list[tuple[int, int, int]] = []
    for phase in phases:
        if not isinstance(phase, dict):
            issues.append("invalid_phase")
            continue
        try:
            phase_indexes.add(int(phase.get("phase_index")))
            day_start = int(phase.get("day_start"))
            day_end = int(phase.get("day_end"))
        except (TypeError, ValueError):
            issues.append("invalid_phase_days")
            continue
        if day_start < 1 or day_end < day_start:
            issues.append("invalid_phase_days")
        phase_days.append((int(phase.get("phase_index")), day_start, day_end))
        final_day = max(final_day, day_end)

        parameters = phase.get("parameters")
        if not isinstance(parameters, list):
            issues.append("invalid_parameters")
            continue
        codes = [
            str(parameter.get("config_code") or "").upper()
            for parameter in parameters
            if isinstance(parameter, dict)
        ]
        if set(codes) < {"TEMP", "HUMID", "TURN", "FAN"}:
            issues.append("missing_required_parameters")
        if len(codes) != len(set(codes)):
            issues.append("duplicate_parameter_codes")
        for parameter in parameters:
            if not isinstance(parameter, dict):
                issues.append("invalid_parameter")
                continue
            code = str(parameter.get("config_code") or "").upper()
            target = _as_float(parameter.get("target_value"))
            minimum = _as_float(parameter.get("min_value"))
            maximum = _as_float(parameter.get("max_value"))
            if target is None or minimum is None or maximum is None:
                issues.append("missing_or_invalid_parameter_value")
                continue
            if minimum > maximum:
                issues.append("invalid_parameter_range")
            if target < minimum:
                issues.append("target_outside_parameter_range")
            if target > maximum:
                issues.append("target_outside_parameter_range")
            if code == "TEMP" and not 35 <= target <= 40:
                issues.append("unsafe_temperature")
            if code == "HUMID":
                if not 30 <= target <= 90:
                    issues.append("unsafe_humidity")
                humidity_targets.append((int(phase.get("phase_index")), target))
            if code == "TURN" and not 0 <= target <= 24:
                issues.append("unsafe_turning")
            if code == "TURN":
                turning_targets.append((int(phase.get("phase_index")), target))
            if code == "FAN" and not 0 <= target <= 10:
                issues.append("unsafe_fan")

    if phase_indexes != {1, 2, 3}:
        issues.append("invalid_phase_indexes")
    if len(phase_days) == 3:
        ordered_days = sorted(phase_days)
        if ordered_days[0][1] != 1 or any(
            current[1] != previous[2] + 1
            for previous, current in zip(ordered_days, ordered_days[1:])
        ):
            issues.append("non_contiguous_phase_days")
    if egg_type in EXPECTED_FINAL_DAY and final_day != EXPECTED_FINAL_DAY[egg_type]:
        issues.append("invalid_final_day")
    if len(humidity_targets) >= 2:
        ordered_humidity = sorted(humidity_targets)
        if ordered_humidity[-1][1] <= ordered_humidity[0][1]:
            issues.append("final_humidity_not_increased")
    if len(turning_targets) >= 2:
        ordered_turning = sorted(turning_targets)
        if ordered_turning[-1][1] > 1 or ordered_turning[-1][1] >= ordered_turning[0][1]:
            issues.append("final_turning_not_reduced")
    return sorted(set(issues))


def _as_float(value: object) -> float | None:
    try:
        parsed = float(value)
    except (TypeError, ValueError):
        return None
    return parsed if math.isfinite(parsed) else None
