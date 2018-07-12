namespace ClientContacts

module DebugUtils =
    open System.Diagnostics
    open System

    let getTime() = DateTime.Now.ToString()
    let (^) a b =
        sprintf "%s%s" a b

    let debug s =
        
        Debug.WriteLine(getTime() ^ " > " ^ s)

