[![Build Status](https://travis-ci.com/hmlendea/stream-to-m3u.svg?branch=master)](https://travis-ci.com/hmlendea/stream-to-m3u)

# About

Tool for retrieving the M3U playlist URL for a given live stream

# Usage

The tool is used as just another console application.

Open a terminal and run the `SteamToM3U` executable, making sure that all the desired arguments are supplied.

# Configuration

The settings are supplied via CLI arguments

The following arguments are used to indicate the source of the stream

| Argument                                                     | Description                     |
|--------------------------------------------------------------|---------------------------------|
| --youtube<br>--yt                                            | Sets the source to YouTube      |
| --twitch                                                     | Sets the source to Twitch       |
| --seenow                                                     | Sets the source to SeeNow       |
| --tvsporthd<br>--tvsport<br>--tvshd<br>--tvs                 | Sets the source to TV Sport HD  |
| --antena-play<br>--antenaplay<br>--antena<br>--aplay<br>--ap | Sets the source to Antena Play  |

If no source argument is provided, a generic solution will be attempted

Each source will require a different set of arugments to indicate the desired live stream, as follows

## YouTube

| Argument        | Description                  | Optional  |
|-----------------|------------------------------|-----------|
| --channel<br>-c | The host channel ID          | Mandatory |
| --title<br>-t   | The title of the live stream | Optional  |

## Twitch

| Argument        | Description                  | Optional  |
|-----------------|------------------------------|-----------|
| --channel<br>-c | The host channel ID          | Mandatory |

## SeeNow

| Argument        | Description                  | Optional  |
|-----------------|------------------------------|-----------|
| --channel<br>-c | The host channel ID          | Mandatory |

## TV Sport HD

| Argument        | Description                  | Optional  |
|-----------------|------------------------------|-----------|
| --channel<br>-c | The host channel ID          | Mandatory |

## Antena Play

| Argument        | Description                  | Optional  |
|-----------------|------------------------------|-----------|
| --channel<br>-c | The host channel ID          | Mandatory |

## Other

| Argument    | Description                                       | Optional  |
|-------------|---------------------------------------------------|-----------|
| --url<br>-u | The URL of the page that contains the live stream | Mandatory |
