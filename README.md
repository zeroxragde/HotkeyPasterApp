Hotkey Paster
Hotkey Paster es una utilidad de productividad para Windows que te permite configurar una combinación de teclas global para pegar instantáneamente un texto predefinido. Para garantizar la máxima seguridad, el texto se almacena de forma encriptada utilizando AES-256, protegiendo tu información confidencial. La aplicación se ejecuta discretamente en la bandeja del sistema (system tray).

Características Principales
Combinación de Teclas Global Configurable: Define tu propio atajo de teclado (ej. Ctrl + F1, Alt + P) para activar la función de pegado en cualquier aplicación de Windows.

Texto a Pegar Configurable y Encriptado: Configura el texto que deseas pegar. Este texto se guarda en un archivo JSON local, pero encriptado con AES-256 utilizando una clave maestra compilada en la aplicación.

Ejecución en la Bandeja del Sistema: La aplicación vive en el tray de Windows, manteniendo tu escritorio limpio y ofreciendo un menú de acceso rápido.

Menú de Contexto: Accede fácilmente a las opciones de configuración y salida desde el icono del tray.

Requisitos del Sistema
Sistema Operativo: Windows 10 o superior.

Framework: .NET 6.0 Runtime o superior (para ejecutar el .exe) o .NET 6.0 SDK (para compilar desde el código fuente).

Instalación y Uso
Opción 1: Usar el Ejecutable Precompilado
Descarga la última versión desde la sección de Releases.

Extrae el archivo .zip en una carpeta de tu elección.

Ejecuta HotkeyPasterApp.exe.

Asegúrate de que el archivo MiIcono.ico esté en la misma carpeta para que se muestre correctamente en el tray.

Opción 2: Compilar desde el Código Fuente
Clona este repositorio: git clone https://github.com/tu-usuario/HotkeyPaster.git.

Abre el proyecto en Visual Studio 2022 o superior.

Asegúrate de tener instalado el SDK de .NET 6.0.

Compila y ejecuta el proyecto (F5).

Configuración
Una vez que la aplicación esté ejecutándose en el tray, haz clic derecho sobre su icono para acceder al menú:

Configurar Clave: Abre un diálogo para introducir el texto que deseas que la aplicación pegue. Este texto se encriptará inmediatamente.

Configurar Combinación: Permite seleccionar el modificador (Ninguno, Alt, Ctrl, Shift) y la tecla (A-Z, F1-F12) para tu atajo global.

Salir: Cierra la aplicación por completo.

Toda la configuración se guarda automáticamente en un archivo config.json en la misma carpeta que el ejecutable.

Seguridad
Hotkey Paster prioriza la seguridad de tu información:

Utiliza el algoritmo de encriptación AES-256 para proteger el texto configurado antes de guardarlo en disco.

La clave de encriptación está compilada directamente en el código de la aplicación, lo que dificulta su extracción para usuarios no técnicos.

Nota Importante: Aunque AES-256 es muy seguro, cualquier persona con acceso al código fuente y conocimientos técnicos podría extraer la clave compilada. Esta herramienta está diseñada para proteger tu texto de accesos casuales o no autorizados en tu propia máquina, no de análisis forenses avanzados.

Contribuciones
¡Las contribuciones son bienvenidas! Si tienes ideas para nuevas características, mejoras de seguridad o correcciones de errores, por favor abre un issue o envía un pull request.

Licencia
Este proyecto está bajo la Licencia MIT. Consulta el archivo LICENSE para más detalles.