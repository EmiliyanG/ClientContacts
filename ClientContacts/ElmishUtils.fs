namespace ClientContacts

module ElmishUtils = 
   open Elmish
   open Elmish.WPF
   open System.Threading
   open System


   type AsyncRequest = {cancelSource: CancellationTokenSource; latestRequest: DateTime}

   let ofAsync (task: Async<_>)
            (cToken:CancellationToken)
            (ofSuccess: _ -> 'msg)
            (ofError: _ -> 'msg) : Cmd<'msg> =
        
        let buildAsync (c:CancellationToken) a = 
            Async.Start(a,c)
        
        let bind dispatch =
            async {
                let! r = task |> Async.Catch
                dispatch (match r with
                            | Choice1Of2 x -> ofSuccess x
                            | Choice2Of2 x -> ofError x)
            }
        [bind >> buildAsync cToken]
    
   let newDate() = DateTime.Now

