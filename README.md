# PepperDash Apc Plugin

> The APC plugin endeavors to provide device control and routing over Apc Type Power devices.

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
            "endOfLineString": "\n",
            "deviceReadyResponsePattern": "",
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
<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 1.15.0
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "Ap89xx",
    "group": "Group",
    "properties": {
        "Control": "SampleValue",
        "PowerCycleTimeMs": 0,
        "Outlets": {
            "SampleString": {
                "Name": "SampleString",
                "OutletIndex": 0,
                "DelayOn": 0,
                "DelayOff": 0,
                "IsInvisible": true
            }
        },
        "UseEssentialsJoinmap": true,
        "enableOutletsOverride": true
    }
}
```
<!-- END Config Example -->
<!-- START Supported Types -->
### Supported Types

- Ap89xx
<!-- END Supported Types -->
<!-- START Join Maps -->
### Join Maps

#### Digitals

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Device Online |
| 50 | R | Outlet Online |
| 100 | R | Outlet Power On/Feedback |
| 150 | R | Outlet Power Off |
| 200 | R | Outlet Power Toggle |

#### Serials

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Device Name |
| 50 | R | Outlet Name |
<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- IOutletName
- IOutletPower
- IOutletOnline
- IHasControlledPowerOutlets
- IQueueMessage
- IApDeviceBuilder
- IApOutlet
- IHasPowerControlWithFeedback
- IKeyName
- IOnline
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- EssentialsBridgeableDevice
- JoinMapBaseAdvanced
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void SendText(string text)
- public void ProcessResponse(ReadOnlyDictionary<int, IHasPowerCycle> outlets, string response)
- public void OutletPowerCycle(uint outletIndex)
- public void ToggleOutletPower(uint outletIndex)
- public bool TryGetOutletNameFeedback(uint outletIndex, out StringFeedback result)
- public bool TryGetOutletOnlineFeedback(uint outletIndex, out BoolFeedback result)
- public bool TryGetOutletPowerFeedback(uint outletIndex, out BoolFeedback result)
- public void TurnOutletOff(uint outletIndex)
- public void TurnOutletOn(uint outletIndex)
- public void Dispatch()
- public EssentialsDevice Build()
- public void PowerOff()
- public void PowerOn()
- public void PowerToggle()
- public void SetIsOnline()
- public void PowerCycle()
- public void PowerOff()
- public void PowerOn()
- public void PowerToggle()
- public void SetIsOnline()
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->
### Bool Feedbacks

- IsOnline
- IsOnline
- PowerIsOnFeedback
- PowerIsOnFeedback
- IsOnline
<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->

<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->
### String Feedbacks

- NameFeedback
- NameFeedback
<!-- END String Feedbacks -->
