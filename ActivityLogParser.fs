module ActivityLogParser

open System
open FSharp.Data

type ActivityLogCsv = CsvProvider<"activity-log-template.csv">
type FileDescriptorJson = JsonProvider<"activity-log-file-description-template.json">

type Activity =
    | FileAdded of path : string 
    | FileEdited of path : string
type User = | User of name : string * email : string
type Event = {Time: DateTime; Activity: Activity; User: User}
    

let parseFileRow (row:ActivityLogCsv.Row) =
    let info = FileDescriptorJson.Parse(row.Info)
    match row.``Event type`` with
    | "Edited files" ->
        Some (FileEdited info.Path)
    | "Added files" ->
        Some (FileAdded info.Path)
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


