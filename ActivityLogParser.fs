module ActivityLogParser

open System
open FSharp.Data


[<Literal>]
let  ActivityLogTemplateFile = __SOURCE_DIRECTORY__ + "/" + "activity-log-template.csv"

type ActivityLogCsv = CsvProvider<ActivityLogTemplateFile>
type FileDescriptorJson = JsonProvider<"""{"path": "/Articles/Interesting article.pdf", "is_dir": false, "file_id": "aGTP3TBWMKARRRAAADAALF", "host_id": 1234567890}""">

type Activity =
    | FileAdded of path : string 
    | FileEdited of path : string
type User = | User of name : string * email : string
type Event = {Time: DateTime; Activity: Activity; User: User}
    

let parseFileRow (row:ActivityLogCsv.Row) =
    let parseInfo () =
        FileDescriptorJson.Parse(row.Info)
    match row.``Event type`` with
    | "Edited files" ->
        Some (FileEdited (parseInfo().Path))
    | "Added files" ->
        Some (FileAdded (parseInfo()).Path)
    | _ ->
        None
        
let parseRow (row:ActivityLogCsv.Row) =
    let user = User (row.Name, row.Email)
    let activity =
        match row.``Event category`` with
        | "Files" ->
            parseFileRow row
        | _ ->
            None
    match activity with
    | Some a ->
        Some {Time=row.Time; Activity=a; User=user}
    | None ->
        None

(* Parse the log and return the rows that we can parse *)
let parseRows rows =
    rows
    |> Seq.map parseRow
    |> Seq.choose id


let parseActivityLog (fname:string) =
    let rows = ActivityLogCsv.Load(fname).Rows
    parseRows rows


