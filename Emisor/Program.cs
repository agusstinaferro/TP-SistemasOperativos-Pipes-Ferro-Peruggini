using System.IO.Pipes;
using System.Text;

// Emisor: el usuario carga los pagos y se mandan por un named pipe. Acá no se enmascara nada, eso lo hace el receptor (otro proceso).
// nombre del pipe, tiene que ser el mismo que usa el receptor
const string NombrePipe = "tp_pagos_sanitizador";
// cuánto espera en ms a que el receptor esté listo antes de rendirse
const int TimeoutConexionMs = 5000;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("Emisor listo.");

// "." significa que el pipe está en esta misma compu
using var cliente = new NamedPipeClientStream(".", NombrePipe, PipeDirection.Out);

try
{
    // se queda esperando hasta que el receptor acepte o pase el timeout
    cliente.Connect(TimeoutConexionMs);
}
catch (TimeoutException)
{
    // si el receptor no estaba abierto, avisamos y salimos
    Console.WriteLine("No hay receptor. Inicie el receptor primero.");
    return 1;
}

Console.WriteLine("Conectado.");

// el StreamWriter nos deja escribir texto en el pipe; AutoFlush manda cada línea enseguida
using var salida = new StreamWriter(cliente, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };

try
{
    EnviarInteractivo(salida);
}
catch (IOException)
{
    // pasa si el receptor se cerró mientras estábamos escribiendo
    Console.WriteLine("Se cortó la conexión.");
    return 1;
}

salida.Flush();
Console.WriteLine("Emisor cerrado.");
return 0;

static void EnviarInteractivo(StreamWriter salida)
{
    // guardo los registros enviados para poder mostrarlos en la tabla
    var registros = new List<Registro>();

    Console.WriteLine("Para finalizar el programa dejar 'Nombre' vacio");
    while (true)
    {
        Console.Write("Nombre: ");
        var nombre = Console.ReadLine();
        // nombre vacío = el usuario quiere terminar
        if (string.IsNullOrWhiteSpace(nombre))
            break;

        // el "?? """ evita problemas si ReadLine devuelve null
        Console.Write("Apellido: ");
        var apellido = (Console.ReadLine() ?? "").Trim();
        Console.Write("DNI: ");
        var dni = (Console.ReadLine() ?? "").Trim();
        Console.Write("Tarjeta: ");
        var tarjeta = (Console.ReadLine() ?? "").Trim();

        // si los datos no sirven, no se envía ni se agrega a la tabla
        if (!Enviar(salida, nombre.Trim(), apellido, dni, tarjeta))
            continue;

        registros.Add(new Registro
        {
            Nombre = nombre.Trim(),
            Apellido = apellido,
            Dni = dni,
            Tarjeta = tarjeta
        });
        MostrarTabla(registros);
    }
}

static bool Enviar(StreamWriter salida, string nombre, string apellido, string dni, string tarjeta)
{
    // reviso que nombre y apellido no vacíos y que no tengan "|", porque ese caracter lo uso para separar los campos
    if (nombre.Length == 0 || apellido.Length == 0
        || nombre.Contains('|') || apellido.Contains('|')
        || dni.Contains('|') || tarjeta.Contains('|'))
    {
        Console.WriteLine("Datos inválidos, verifique el registro.");
        return false;
    }

    // un registro = una línea.  (el pipe es un flujo de bytes, no sabe separar mensajes solo)
    salida.WriteLine($"{nombre}|{apellido}|{dni}|{tarjeta}");
    Console.WriteLine("Enviado.");
    return true;
}

static void MostrarTabla(List<Registro> registros)
{
    // ancho de cada columna
    const int ancho = 16;
    Console.WriteLine();
    // encabezado
    Console.WriteLine(
        "Nombre".PadRight(ancho) +
        "Apellido".PadRight(ancho) +
        "DNI".PadRight(ancho) +
        "Tarjeta");
    // una fila por cada registro enviado
    foreach (var registro in registros)
    {
        Console.WriteLine(
            registro.Nombre.PadRight(ancho) +
            registro.Apellido.PadRight(ancho) +
            registro.Dni.PadRight(ancho) +
            registro.Tarjeta);
    }

    Console.WriteLine();
}

// guarda los datos de un pago
class Registro
{
    public string Nombre = "";
    public string Apellido = "";
    public string Dni = "";
    public string Tarjeta = "";
}
