#load "packages/FsLab/Themes/DefaultWhite.fsx"
#load "packages/FsLab/FsLab.fsx"
#r "MathNet.Numerics.dll"

open System
open System.IO
open Deedle
open FSharp.Data
open XPlot.GoogleCharts
open XPlot.GoogleCharts.Deedle
open MathNet.Numerics

#load "ActivityLogParser.fs"
open ActivityLogParser

let numberOfEditsByDay (events:Event seq) =
    [for k,v in (events |> Seq.countBy (fun e -> e.Time.Date)) -> (k,v)]
    |> series

let chartNumberOfEditsByDay events =
    let options =
        Options(
            title = "File Edits Per Day",
            height = 350,
            width = 1000
            )
    events
    |> numberOfEditsByDay
    |> Chart.Calendar
    |> Chart.WithOptions options


let getArgs () =
    match fsi.CommandLineArgs with
    | [|_; logfile|] ->
        logfile
    | _ ->
        failwithf "One parameter needed: ACTIVITY-LOG-FILENAME"

let filename = getArgs ()


match parseActivityLog filename with
| Some error, _ ->
    printfn "** ERROR: %s" error
| None, events ->
    let chart = chartNumberOfEditsByDay events
    chart.Show()
