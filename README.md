# Metatron
### StealthBot Reborn

Metatron is a bot for the game EVE Online that is designed to be a replacement for StealthBot, which is no longer maintained. Given how long StealthBot was abandoned, it is currently in the Beta stage while the remaining modern features are implemented and bugs are fixed. It requires both Innerspace and ISXEVE to run.

## Features

* **Fleet Management** - Metatron can manage fleets, including broadcasting fleet commands, accepting fleet invites, and joining fleets.
* **Mining** - Metatron can mine asteroids, including using mining drones and strip miners.
* **Ratting** - Metatron can kill NPCs, including using drones.
* **Salvaging** - Metatron can salvage wrecks, loot containers, and use tractor beams.
* **Freighting** - Metatron can haul items between stations.
* **Combat Assist** - Metatron can run only combat modules, including using drones, allowing the player to retain navigation control. This is useful for missions and wormhole combat sites.
* **Mining Boosts** - Metatron can boost fleets using Orca/Porpoise/Rorqual mining boosts (compression not yet supported).
* **Missions!!!** - Metatron can run any and all missions types, with most missions being supported out of the box. Please see below for unsupported missions.
  
## Installation

1. Visit https://metatronbot.com and follow the setup instructions.

## Known Issues

* Dedicated salvager cannot determine if a site is safe or not. You should only start this mode once all sites are cleared.
* Compression is not yet supported.
* Starting the bot during a Critical Move (docking, undocking, jumping, etc.) will cause the bot to get stuck. You must restart the bot to fix this.
* Anomaly ratting does not support all site types. If you find a site that is not supported, please contact me.
* Extra alliance/corp information is not available. StealthBot relied on connections to the old EVE XML API, which is no longer available. This information will be added once the new ESI API is implemented.
* Metatron currently has no way to update itself. This will be implemented in a future release. Until then, you will need to manually update the bot by cloning the latest release from GitHub.
* Metatron will sometimes miss picking up mission items due to lag. The bot will attempt to turn in the incomplete mission, realize it's incomplete, and then go and try again. This process will be improved in a future release.
* Not all missions are supported. Known incompatible missions: Anomic Base/Team/Agent, The Anomaly, Are you Receiving?
