# TP Integrador - Sistemas Operativos (2C 2026)

**Opción 8: IPC con Pipes (Tuberías)** — *Pipeline de Seguridad y Sanitización de Datos Financieros*

## Integrantes
* Agustina Ferro
* Naiara Peruggini

## Descripción
Solución en C# (.NET) sobre Windows que comunica dos procesos independientes (`Emisor` y `Receptor`) mediante un **Named Pipe** (`\\.\pipe\tp_pagos_sanitizador`). 
El emisor recibe los datos sensibles por teclado y el receptor los valida, enmascara DNI y tarjeta (dejando visibles solo los últimos 4 dígitos) y los registra en `sanitizado.log`.

## Ejecución
1. Abrir dos terminales en la carpeta raíz del proyecto.
2. Iniciar primero el servidor/receptor:
   `dotnet run --project Receptor`
3. Iniciar el cliente/emisor en la otra terminal:
   `dotnet run --project Emisor`
