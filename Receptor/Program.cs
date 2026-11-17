using System.IO.Pipes;
using System.Text;

// Receptor: lee los datos que llegan por el named pipe, enmascara DNI y tarjeta 
//(solo se ven los últimos 4 dígitos) y los muestra en una tabla indicando
// si el registro se aceptó o se rechazó. También guarda todo en un log.

// mismo nombre de pipe que el emisor
const string NombrePipe = "tp_pagos_sanitizador";
// tamaño del buffer del pipe (lo reserva el sistema operativo)
const int BufferBytes = 4096;
const string ArchivoLog = "sanitizado.log";

Console.OutputEncoding = Encoding.UTF8;
var rutaLog = Path.GetFullPath(ArchivoLog);

Console.WriteLine("Esperando al emisor.");

// creo el pipe; acá el receptor es el servidor y solo lee (PipeDirection.In)
using var servidor = new NamedPipeServerStream(
    NombrePipe,
    PipeDirection.In,
    maxNumberOfServerInstances: 1,
    PipeTransmissionMode.Byte,
    PipeOptions.None,
    inBufferSize: BufferBytes,
    outBufferSize: BufferBytes);

// se queda bloqueado esperando hasta que el emisor se conecte
servidor.WaitForConnection();
Console.WriteLine("Emisor conectado.");
Console.WriteLine("Leyendo datos.");

// el StreamReader permite leer el pipe línea por línea
using var lector = new StreamReader(servidor, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
// el log se va escribiendo al final del archivo (append)
using var log = new StreamWriter(rutaLog, append: true, Encoding.UTF8) { AutoFlush = true };

// acá voy guardando todos los registros recibidos para armar la tabla
var registros = new List<Registro>();

try
{
    string? linea;
    // ReadLine espera hasta que llegue una línea completa. Cuando el emisor cierra el pipe devuelve null (fin de archivo) y el while termina.
    while ((linea = lector.ReadLine()) != null)
    {
        if (string.IsNullOrWhiteSpace(linea))
            continue;

        // valido y enmascaro el registro
        var registro = Sanitizar(linea);
        registros.Add(registro);

        // muestro la tabla actualizada
        MostrarTabla(registros);

        // y lo guardo en el log con fecha y hora
        log.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {registro.Estado} | {registro.Nombre} {registro.Apellido} | DNI {registro.Dni} | Tarjeta {registro.Tarjeta}");
    }
}
catch (IOException)
{
    // si el pipe se rompe a mitad de camino
    Console.WriteLine("Se cortó la conexión.");
    return 1;
}

Console.WriteLine("Emisor cerrado.");
return 0;

static Registro Sanitizar(string trama)
{
    // separo los campos por el "|"
    var partes = trama.Split('|');
    // si no vinieron los 4 campos, la trama está mal y se rechaza
    if (partes.Length != 4)
    {
        return new Registro
        {
            Nombre = "-",
            Apellido = "-",
            Dni = "-",
            Tarjeta = "-",
            Estado = "RECHAZADO"
        };
    }

    var nombre = partes[0].Trim();
    var apellido = partes[1].Trim();
    var dni = partes[2].Trim();
    var tarjeta = partes[3].Trim();

    // acá voy anotando lo que está mal; solo me interesa saber si hay alguno
    var errores = new List<string>();
    // me quedo solo con los números para poder contarlos
    var digitosDni = SoloDigitos(dni);
    var digitosTarjeta = SoloDigitos(tarjeta);

    if (apellido.Length == 0)
        errores.Add("falta el apellido");

    if (digitosDni.Length < 7 || digitosDni.Length > 8)
        errores.Add("el DNI debe tener 7 u 8 dígitos");

    if (digitosTarjeta.Length < 13 || digitosTarjeta.Length > 19)
        errores.Add("la tarjeta debe tener entre 13 y 19 dígitos");

    // si no hubo ningún error se acepta, si hubo alguno se rechaza
    var estado = errores.Count == 0 ? "ACEPTADO" : "RECHAZADO";

    // aunque se rechace, el DNI y la tarjeta salen siempre enmascarados
    return new Registro
    {
        Nombre = nombre,
        Apellido = apellido,
        Dni = Enmascarar(dni),
        Tarjeta = Enmascarar(tarjeta),
        Estado = estado
    };
}

static void MostrarTabla(List<Registro> registros)
{
    // ancho de cada columna (la tarjeta es más ancha porque tiene más dígitos)
    const int anchoNombre = 16;
    const int anchoDni = 14;
    const int anchoTarjeta = 22;

    Console.WriteLine();
    // encabezado
    Console.WriteLine(
        "Nombre".PadRight(anchoNombre) +
        "Apellido".PadRight(anchoNombre) +
        "DNI".PadRight(anchoDni) +
        "Tarjeta".PadRight(anchoTarjeta) +
        "Estado");
    // una fila por cada registro recibido
    foreach (var registro in registros)
    {
        Console.WriteLine(
            registro.Nombre.PadRight(anchoNombre) +
            registro.Apellido.PadRight(anchoNombre) +
            registro.Dni.PadRight(anchoDni) +
            registro.Tarjeta.PadRight(anchoTarjeta) +
            registro.Estado);
    }

    Console.WriteLine();
}

static string Enmascarar(string valor)
{
    var chars = valor.ToCharArray();
    // guardo en qué posiciones hay dígitos (no miro espacios, puntos y guiones)
    var indices = new List<int>();
    for (var i = 0; i < chars.Length; i++)
    {
        if (char.IsDigit(chars[i]))
            indices.Add(i);
    }

    // tapo todos los dígitos menos los últimos 4
    var ocultar = Math.Max(0, indices.Count - 4);
    for (var i = 0; i < ocultar; i++)
        chars[indices[i]] = '*';

    return new string(chars);
}

static string SoloDigitos(string valor)
{
    // recorro el texto y me quedo únicamente con los números
    var digitos = new StringBuilder(valor.Length);
    foreach (var c in valor)
    {
        if (char.IsDigit(c))
            digitos.Append(c);
    }

    return digitos.ToString();
}

// guarda los datos de un pago ya procesado
class Registro
{
    public string Nombre = "";
    public string Apellido = "";
    public string Dni = "";
    public string Tarjeta = "";
    public string Estado = "";
}