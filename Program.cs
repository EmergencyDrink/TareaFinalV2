using System;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var cts = new CancellationTokenSource();
        var HacerPedido = ProcesarPedidoAsync(cts.Token);

        // Cancelar si tarda más de 15 segundos
        var TiempoEspera = Task.Delay(15000);
        var Taskcompleta = await Task.WhenAny(HacerPedido,  TiempoEspera);

        if (Taskcompleta == TiempoEspera)
        {
        // Con menos tiempo se puede verificar que el programa entra aqui
            cts.Cancel();
            Console.WriteLine("El procesamiento del pedido ha sido cancelado por tiempo excedido.");
        }

        Console.WriteLine("Proceso finalizado.");
    }

    static Task ProcesarPedidoAsync(CancellationToken cancellationToken)
    {
        var cts = new CancellationTokenSource();
        return Task.Run(async () =>
        {
            var pagoTask = ValidarPagoAsync();

            var inventarioTask = pagoTask.ContinueWith(t =>
            {
                return Task.Factory.StartNew(() => VerificarInventarioAsync(),
                    cancellationToken, 
                    TaskCreationOptions.AttachedToParent, 
                    TaskScheduler.Default).Unwrap();
            }, TaskContinuationOptions.OnlyOnRanToCompletion).Unwrap();

            var facturaTask = inventarioTask.ContinueWith(t =>
            {
                return GenerarFacturaAsync();
            }, TaskContinuationOptions.OnlyOnRanToCompletion).Unwrap();

            // Manejo de cancelación si el pago falla
            pagoTask.ContinueWith(t =>
            {
                Console.WriteLine("Error: Pago no válido. Pedido cancelado.");
                cts.Cancel();
            }, TaskContinuationOptions.OnlyOnCanceled);

            await facturaTask;
        }, cancellationToken);
    }

    static async Task<bool> ValidarPagoAsync()
    {
        await Task.Delay(2000); // Simula validación
        var pagoExitoso = new Random().Next(0, 10) <8; // 80% de probabilidad de exito
        Console.WriteLine(pagoExitoso);
        if (!pagoExitoso)
        {
            throw new TaskCanceledException("Pago fallido.");
        }
        Console.WriteLine("Pago validado.");
        return true;
    }

    static async Task<bool> VerificarInventarioAsync()
    {
        await Task.Delay(2500); // Simula consulta de inventario
        Console.WriteLine("Inventario verificado.");
        return true;
    }

    static async Task GenerarFacturaAsync()
    {
        await Task.Delay(1000); // Simula generación de factura
        Console.WriteLine("Factura generada.");
    }
}

