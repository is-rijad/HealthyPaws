from datetime import datetime, timezone


def optional_field(message, field_name: str):
    """Vrijednost proto3 `optional` polja, ili None ako nije eksplicitno postavljeno."""
    return getattr(message, field_name) if message.HasField(field_name) else None


def to_utc_datetime(timestamp) -> datetime:
    return timestamp.ToDatetime(tzinfo=timezone.utc)
