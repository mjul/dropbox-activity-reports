#r "./packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#load "ActivityLogParser.fs"

open ActivityLogParser
open System


// Set this to something that fits or specify the file on the command line
let DEFAULT_FILENAME = "2016-12-13 full logfile.csv"

(* Create some reports *)

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


let printMostEditedFilesByMonth events =
    for date, path, cnt in (fileEditsByMonth events) do
        if cnt >= 25 then
            printfn "%s\t%s\t%d" (date.ToShortDateString()) path cnt

let filename =
    match fsi.CommandLineArgs with
    | [|_; logfile|] ->
        logfile
    | _ ->
        DEFAULT_FILENAME

match parseActivityLog filename with
| Some error, _ ->
    printfn "** ERROR: %s" error
| None, events ->
    printMostEditedFilesByMonth events
