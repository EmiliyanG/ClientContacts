
// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open ClientContactViews
open ClientContacts
open Elmish
open Elmish.WPF
open System
open System.Windows
open System.Windows.Forms
open ClientContacts.MySQLConnection

[<EntryPoint;STAThread>]
let main argv = 
    
    let window = ClientContactViews.MainWindow()
    
    Program.mkProgram MainWindow.init MainWindow.update MainWindow.view
    |> Program.withErrorHandler (fun (_, ex) -> MessageBox.Show( sprintf "%s\n%s" ex.Message ex.StackTrace ) |> ignore)
    //|> Program.withConsoleTrace
    //|> Program.withSubscription (ContactList.getListOfContacts "")
    |> Program.runWindow ( window )


    

    