namespace ClientContacts
module ConfigManager=
    open System.Configuration
    ///returns the first connection string from the list of connections
    let getConnection = 
        let conns = ConfigurationManager.ConnectionStrings

        match conns.Count with 
        |0 -> failwith "no connection string was provided in the app.config"
        |i when i > 1 -> 
            failwith "multiple connection strings provided in the app.config. Expecting only 1."
        |_ -> ()

        conns.Item 0
        |> fun c -> c.ConnectionString


    

