from app.services.synthetic_validation import validate_synthetic_record


def _valid_record():
    return {
        "egg_type": "chicken",
        "total_eggs": 100,
        "expected_success_rate": 0.84,
        "phases": [
            {
                "phase_index": index,
                "day_start": day_start,
                "day_end": day_end,
                "parameters": [
                    {"config_code": "TEMP", "target_value": 37.5, "min_value": 37.0, "max_value": 38.0},
                    {
                        "config_code": "HUMID",
                        "target_value": 70 if index == 3 else 60,
                        "min_value": 55,
                        "max_value": 75,
                    },
                    {
                        "config_code": "TURN",
                        "target_value": 0 if index == 3 else 6,
                        "min_value": 0,
                        "max_value": 8,
                    },
                    {"config_code": "FAN", "target_value": 2, "min_value": 1, "max_value": 3},
                ],
            }
            for index, (day_start, day_end) in enumerate([(1, 7), (8, 18), (19, 21)], start=1)
        ],
    }


def test_valid_synthetic_record_has_no_issues() -> None:
    assert validate_synthetic_record(_valid_record()) == []


def test_incomplete_synthetic_record_is_rejected() -> None:
    record = _valid_record()
    record["phases"] = record["phases"][:1]

    assert "invalid_phase_count" in validate_synthetic_record(record)


def test_unsafe_synthetic_target_is_rejected() -> None:
    record = _valid_record()
    record["phases"][0]["parameters"][0]["target_value"] = 45

    assert "unsafe_temperature" in validate_synthetic_record(record)


def test_synthetic_record_requires_complete_finite_parameter_ranges() -> None:
    record = _valid_record()
    record["phases"][0]["parameters"][0]["min_value"] = float("nan")
    record["phases"][1]["parameters"] = record["phases"][1]["parameters"][:-1]

    issues = validate_synthetic_record(record)

    assert "missing_or_invalid_parameter_value" in issues
    assert "missing_required_parameters" in issues


def test_synthetic_record_requires_contiguous_days_and_reduced_final_turning() -> None:
    record = _valid_record()
    record["phases"][1]["day_start"] = 9
    record["phases"][2]["parameters"][2]["target_value"] = 6

    issues = validate_synthetic_record(record)

    assert "non_contiguous_phase_days" in issues
    assert "final_turning_not_reduced" in issues
