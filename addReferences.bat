".paket\paket.exe" install

".paket\paket.exe" add Dapper --project ClientContacts
".paket\paket.exe" add Elmish.WPF --project ClientContacts
".paket\paket.exe" add System.Windows.Interactivity.WPF --project ClientContactViews
".paket\paket.exe" add System.Windows.Interactivity.WPF --project ClientContacts