# PepperDash Apc Plugin

This Essentials plugin provides outlet control for Apc Type Power devices.

## Types

1. Ap89xx

## Join Map

### Digitals

| Join    | To Simpl               | From Simpl               |
| ------- | ---------------------- | ------------------------ |
| 1       | Device Online          | -                        |
| 2-50    | Reserved For Future    | Reserved For Future      |
| 51-100  | Power On Feedback      | Power On                 |
| 100-151 | -                      | Power Off                |
| 151-200 | -                      | Power Toggle             |

### Analogs

| Join    | To Simpl               | From Simpl               |
| ------- | ---------------------- | ------------------------ |
| 1       | -                      | -                        |
| 2-50    | -                      | -                        |
| 51-100  | -                      | -                        |
| 100-151 | -                      | -                        |
| 151-200 | -                      | -                        |

### Serials

| Join    | To Simpl               | From Simpl               |
| ------- | ---------------------- | ------------------------ |
| 1       | Device Name            | -                        |
| 2-50    | -                      | -                        |
| 51-100  | Outlet Name            | -                        |
| 100-151 | -                      | -                        |
| 151-200 | -                      | -                        |

### Join Details

---

1. Outlet Name is defined by the Name property in config, and is not necesarily what will be in the APC software; and the APC software requires outlet names to no contain spaces or characters.

## Config Example

```JSON
{
    "key": "PowerSupply01",
    "uid": 74,
    "name": "PowerSupply01",
    "type": "Ap89xx",
    "group": "power",
    "properties": {
        "control": {
            "method": "ssh",
            "tcpSshProperties": {
                "address": "0.0.0.0",
                "port": 22,
                "autoReconnect": true,
                "AutoReconnectIntervalMs": 10000,
                "username": "apc",
                "password": "apc"
            }
        },
        "outlets":
        {
            "outlet01" : {
                "name": "My First Outlet",
                "outletIndex": 1,
                "delayOn": 30,
                "delayOff": 2
            },
            "outlet02" : {
                "name": "Another Awesome Outlet",
                "outletIndex": 4,
                "delayOn": 30,
                "delayOff": 2
            }
        }
    }
}
```

### Config Details

---
__Properties:__

#### "outlets"

- Dictionary that defines the outlets to be controlled
- "key" - defines and sets the name of the outlet in the APC software.  This value must be unique and contain no spaces or specials charaters
- "name" - defines the name that will be sent to the bridge for a UI friendly name.  If this value is not set it will be the key
- "outletIndex" - outletNumber to be controlled
- "delayOn" - NOT YET IMPLEMENTED
- "delayOff" - NOT YET IMPLEMENTED

## Planned Updates

1. Create a custom StatusMonitor to get more detailed infomation
1. Add ability to set on/off delays by outlet
