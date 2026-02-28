using System;
using System.IO;
using CompraProgramada.Api.Services;
using Xunit;

namespace CompraProgramada.Tests
{
    public class MotorAgendamentoStatusStoreTests
    {
        [Fact]
        public void Store_DevePersistirERecuperarEstado_AposRestart()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "compra-programada-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var filePath = Path.Combine(tempDir, "motor-state.json");

            var store1 = new MotorAgendamentoStatusStore(filePath);
            store1.RegistrarSucesso("202602-5", new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc));
            store1.RegistrarFalha(new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc), "erro teste");

            var store2 = new MotorAgendamentoStatusStore(filePath);
            var snapshot = store2.ObterSnapshot();

            Assert.Equal(1, snapshot.TotalExecucoesSucesso);
            Assert.Equal(1, snapshot.TotalExecucoesFalha);
            Assert.Equal("FALHA", snapshot.UltimoResultado);
            Assert.Equal("erro teste", snapshot.UltimoErro);
            Assert.Contains("202602-5", snapshot.CiclosExecutados);

            try { Directory.Delete(tempDir, true); } catch { }
        }
    }
}
