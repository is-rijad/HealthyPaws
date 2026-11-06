from datetime import datetime, timezone


def _opt(msg, field: str):
    return getattr(msg, field) if msg.HasField(field) else None


def _dt(ts) -> datetime:
    return ts.ToDatetime(tzinfo=timezone.utc)