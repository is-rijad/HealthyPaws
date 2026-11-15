class ContractError(ValueError):
    """Nevaljana poruka (otrovna): Broker je šalje u DLQ, ne vraća u red."""