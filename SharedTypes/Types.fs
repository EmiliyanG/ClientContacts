namespace SharedTypes

type EntityType=
    |Organisation
    |Location

type Entity={entityType: EntityType; id:int; name:string}

