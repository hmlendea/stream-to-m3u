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

## Running in background as a service

**Note:** The following instructions only apply for *Linux* distributions using *systemd*.

Create the following service file: /usr/lib/systemd/system/stream-to-m3u@.service
```
[Unit]
Description=Stream to M3U (%i channels)

[Service]
WorkingDirectory=[ABSOLUTE_PATH_TO_SERVICE_DIRECTORY]
ExecStart=[ABSOLUTE_PATH_TO_SERVICE_DIRECTORY]/StreamToM3U -i [ABSOLUTE_PATH_TO_SERVICE_DIRECTORY]/Data/input-%i.xml -O /srv/http/iptv/livestreams/%i -u http://mydomain.com/iptv
MemoryAccounting=yes
MemoryMax=256M

[Install]
WantedBy=multi-user.target
```

Create the following timer file: /lib/systemd/system/stream-to-m3u.timer
```
[Unit]
Description=Periodically creates an M3U playlist out of livestreams (%i channels)

[Timer]
OnBootSec=3min
OnUnitActiveSec=40min

[Install]
WantedBy=timers.target
```

Values that you might want to change:
 - *OnBootSec*: the delay before the service is started after the OS is booted
 - *OnUnitActiveSec*: how often the service will be triggered
 - *MemoryMax*: RAM usage limit

In the above example, the service will start 3 minutes after boot, and then again once every 40 minutes, being allocated 256M RAM per instance
