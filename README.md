# ServerOptimizer v1.0.1
## English
## Description
Optimizes server FPS dynamically based on player count. Reduces CPU usage when server is empty.

https://github.com/Jaie55/ServerOptimizer
https://umod.org/plugins/ZkgZwan5WM
https://codefling.com/plugins/serveroptimizer

## Features
- Dynamic FPS adjustment based on player count
- Configurable minimum FPS when server is empty
- Gradual FPS increment per additional player
- Admin notifications when FPS changes
- Simple admin commands

## Commands
- `/so.status` - Shows current FPS status and configuration
- `/so.toggle` - Enables/disables dynamic FPS adjustment
- `/so.help` - Shows help and available commands

You can also use longer aliases like `/serveroptimizer.status`, etc.

## Permissions
- `serveroptimizer.admin` - Access to all commands and functions
- `serveroptimizer.status` - Allows viewing current FPS status
- `serveroptimizer.toggle` - Allows enabling/disabling dynamic adjustment

All server admins have access to all functions without additional permission configuration.

## Configuration
The plugin automatically creates a configuration file with the following parameters:

- `DynamicFpsEnabled`: Enable/disable dynamic adjustment (default: true)
- `EmptyServerFps`: FPS when there are no players (default: 9)
- `BaseFps`: FPS base with at least one player (default: 26)
- `MaxFps`: Maximum FPS limit (default: 60)
- `FpsIncrementPerPlayer`: FPS increment per player (default: 1.5)
- `CheckInterval`: Check interval in seconds (default: 30)
- `ShowDebugMessages`: Show debug messages (default: false)
- `NotifyFpsChange`: Notify admins of FPS changes (default: true)
- `ChatPrefix`: Prefix for plugin messages
- `DefaultLanguage`: Default language (default: en)
- `ForceInitialization`: Force plugin initialization (default: true)
- `ShowInitMessage`: Show message on initialization (default: true)

## Language Support
The plugin supports the following language codes:
- `en` - English
- `en-PT` - Pirate English
- `es` - Latin American Spanish
- `es-ES` - Spain Spanish
- `ca` - Catalan
- `af` - Afrikaans
- `ar` - Arabic
- `zh-CN` - Chinese (Simplified)
- `zh-TW` - Chinese (Traditional)
- `cs` - Czech
- `da` - Danish
- `nl` - Dutch
- `fi` - Finnish
- `fr` - French
- `de` - German
- `el` - Greek
- `he` - Hebrew
- `hu` - Hungarian
- `it` - Italian
- `ja` - Japanese
- `ko` - Korean
- `no` - Norwegian
- `pl` - Polish
- `pt` - Portuguese
- `pt-BR` - Brazilian Portuguese
- `ro` - Romanian
- `ru` - Russian
- `sr-Cyrl` - Serbian (Cyrillic)
- `sv` - Swedish
- `tr` - Turkish
- `uk` - Ukrainian
- `vi` - Vietnamese

**Note:** Language can only be changed by editing the configuration file. The language setting affects all plugin messages.

## Installation
1. Upload ServerOptimizer.cs to your plugins folder
2. Restart the server or reload plugins
3. The plugin will configure itself with default values

## License
MIT License - Copyright (c) 2025 Jaie55

## Performance Notes
- The plugin is designed to have minimal performance impact
- FPS checks occur every 30 seconds (configurable)
- The plugin properly cleans up resources when unloaded
- FPS formula: FPS = BaseFps   (Players * IncrementPerPlayer), limited by MaxFps

---

# ServerOptimizer v1.0.1 (Español)
## Español (Spanish)
## Descripción
Optimiza los FPS del servidor según el número de jugadores. Reduce el uso de CPU cuando está vacío.

## Características
- Ajuste dinámico de FPS basado en la cantidad de jugadores
- FPS mínimo configurable cuando el servidor está vacío
- Incremento gradual de FPS por cada jugador adicional
- Notificaciones a administradores cuando cambian los FPS
- Comandos sencillos para administradores

## Comandos
- `/so.status` - Muestra el estado actual de los FPS y la configuración
- `/so.toggle` - Activa o desactiva el ajuste dinámico de FPS
- `/so.help` - Muestra la ayuda y comandos disponibles

También puedes usar alias más largos como `/serveroptimizer.status`, etc.

## Permisos
- `serveroptimizer.admin` - Acceso a todos los comandos y funciones
- `serveroptimizer.status` - Permite ver el estado actual de los FPS
- `serveroptimizer.toggle` - Permite activar/desactivar el ajuste dinámico

Todos los administradores tienen acceso a todas las funciones sin necesidad de configuración adicional de permisos.

## Configuración
El plugin crea automáticamente un archivo de configuración con los siguientes parámetros:

- `DynamicFpsEnabled`: Activa/desactiva el ajuste dinámico (predeterminado: true)
- `EmptyServerFps`: FPS cuando no hay jugadores (predeterminado: 9)
- `BaseFps`: FPS base cuando hay al menos un jugador (predeterminado: 26)
- `MaxFps`: Límite máximo de FPS (predeterminado: 60)
- `FpsIncrementPerPlayer`: Incremento de FPS por jugador (predeterminado: 1.5)
- `CheckInterval`: Intervalo de verificación en segundos (predeterminado: 30)
- `ShowDebugMessages`: Muestra mensajes de depuración (predeterminado: false)
- `NotifyFpsChange`: Notifica cambios de FPS a administradores (predeterminado: true)
- `ChatPrefix`: Prefijo para mensajes del plugin
- `DefaultLanguage`: Idioma predeterminado (predeterminado: en)
- `ForceInitialization`: Fuerza inicialización del plugin (predeterminado: true)
- `ShowInitMessage`: Muestra mensaje al inicializar (predeterminado: true)

## Soporte de idiomas
El plugin soporta los siguientes códigos de idioma:
- `en` - Inglés
- `en-PT` - Inglés Pirata
- `es` - Español Latinoamericano
- `es-ES` - Español de España
- `ca` - Catalán
- `af` - Afrikáans
- `ar` - Árabe
- `zh-CN` - Chino Simplificado
- `zh-TW` - Chino Tradicional
- `cs` - Checo
- `da` - Danés
- `nl` - Holandés
- `fi` - Finés
- `fr` - Francés
- `de` - Alemán
- `el` - Griego
- `he` - Hebreo
- `hu` - Húngaro
- `it` - Italiano
- `ja` - Japonés
- `ko` - Coreano
- `no` - Noruego
- `pl` - Polaco
- `pt` - Portugués
- `pt-BR` - Portugués Brasileño
- `ro` - Rumano
- `ru` - Ruso
- `sr-Cyrl` - Serbio (Cirílico)
- `sv` - Sueco
- `tr` - Turco
- `uk` - Ucraniano
- `vi` - Vietnamita

**Nota:** El idioma solo se puede cambiar editando el archivo de configuración. La configuración de idioma afecta a todos los mensajes del plugin.

## Instalación
1. Sube el archivo ServerOptimizer.cs a la carpeta plugins
2. Reinicia el servidor o recarga los plugins
3. El plugin se configurará automáticamente con valores predeterminados

## Licencia
MIT License - Copyright (c) 2025 Jaie55

## Notas de rendimiento
- El plugin está diseñado para tener un impacto mínimo en el rendimiento
- Las verificaciones de FPS ocurren cada 30 segundos (configurable)
- El plugin limpia correctamente los recursos al descargar
- Fórmula FPS: FPS = BaseFps   (Jugadores * IncrementoFPS), limitado por MaxFps
