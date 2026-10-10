# ParqueaderoMoto (.NET MAUI 10)

Proyecto base limpio para administrar un parqueadero de motos en **Windows y Android**.

## Incluye
- Capacidad de 20 puestos.
- Registro de entradas con placa, cliente opcional, puesto y hora automática.
- Validación de placa activa duplicada, puesto ocupado y parqueadero lleno.
- Registro de salida y cálculo por hora iniciada (redondea hacia arriba; mínimo una hora).
- Métodos de pago: efectivo, transferencia y tarjeta.
- Ingresos del día, motos activas e historial reciente.
- Base de datos local SQLite persistente en el dispositivo.

## Abrir
1. Extrae el ZIP.
2. Abre `ParqueaderoMoto.csproj` en Visual Studio 2026 con la carga de trabajo de .NET MAUI instalada.
3. Espera la restauración de NuGet.
4. Selecciona Windows Machine o un dispositivo/emulador Android y ejecuta.

## Tarifa
La tarifa inicial es de **$3.000 COP por hora**, editable desde la pantalla de salida. Ajusta este valor al precio real del negocio.

## Importante
Esta entrega contiene el código fuente del proyecto; no incluye un instalador Windows ni un APK ya compilado. Para generar esos paquetes se debe compilar en Visual Studio/.NET con los workloads correspondientes.
