#r "./packages/FSharp.Data/lib/net40/FSharp.Data.dll"

open System
open FSharp.Data


// Set this to something that fits or specify the file on the command line
let DEFAULT_FILENAME = "2016-12-13 full logfile.csv"


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

let firstDayOfMonth (d:DateTime) =
    new DateTime(d.Year, d.Month, 1)

let fileEditsByMonth (events : Event seq) =
    seq {
        for e in events do
        match e.Activity with
        | FileEdited path ->
            yield ((firstDayOfMonth e.Time), path)
        | _ ->
            ignore()
        }
    |> Seq.sort
    |> Seq.countBy id
    |> Seq.map (fun ((month, path), cnt) -> (month,path,cnt))


let printMostEditedFilesByMonth (fname:string) =
    let rows = ActivityLogCsv.Load(fname).Rows
    let parsed = parseRows rows
    for date, path, cnt in (fileEditsByMonth parsed) do
        if cnt >= 25 then
            printfn "%s\t%s\t%d" (date.ToShortDateString()) path cnt
    

let filename =
    match fsi.CommandLineArgs with
    | [|_; logfile|] ->
        logfile
    | _ ->
        DEFAULT_FILENAME

if not (System.IO.File.Exists(filename)) then
    failwithf "Activity log not found: %s" filename
else
    printMostEditedFilesByMonth filename
