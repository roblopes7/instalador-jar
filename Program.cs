using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;

class Program
{
    public class ServiceConfig
    {
        public string ServiceName { get; set; }
        public string JarPath { get; set; }
        public string Description { get; set; }
    }

    static void Main(string[] args)
    {
        string configPath = "config.json";
        if (!File.Exists(configPath))
        {
            Console.WriteLine("Arquivo de configuração 'config.json' não encontrado.");
            return;
        }

        try
        {
            // Ler configurações do JSON
            ServiceConfig config = JsonSerializer.Deserialize<ServiceConfig>(File.ReadAllText(configPath));
            if (config == null)
            {
                Console.WriteLine("Erro ao carregar as configurações do arquivo JSON.");
                return;
            }

            string currentDir = Directory.GetCurrentDirectory();
            string executablePath = Path.Combine(currentDir, $"{config.ServiceName}.exe");
            string xmlConfigPath = Path.Combine(currentDir, $"{config.ServiceName}.xml");

            // Baixar o WinSW
            DownloadWinSW(executablePath);

            // Criar o arquivo de configuração XML
            CreateConfigFile(config, xmlConfigPath);

            // Instalar o serviço
            InstallService(executablePath);

            // Iniciar o serviço
            StartService(executablePath);

            Console.WriteLine("Serviço instalado e iniciado com sucesso.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ocorreu um erro: {ex.Message}");
        }
    }

    static void DownloadWinSW(string destPath)
    {
        string url = "https://github.com/winsw/winsw/releases/latest/download/winsw-x64.exe";
        using (var client = new System.Net.WebClient())
        {
            client.DownloadFile(url, destPath);
        }
        Console.WriteLine($"WinSW baixado com sucesso em {destPath}.");
    }

    static void CreateConfigFile(ServiceConfig config, string configPath)
    {
        string xmlContent = $@"
<service>
  <id>{config.ServiceName}</id>
  <name>{config.ServiceName}</name>
  <description>{config.Description}</description>
  <executable>java</executable>
  <arguments>-jar {config.JarPath}</arguments>
  <logmode>rotate</logmode>
</service>";
        File.WriteAllText(configPath, xmlContent);
        Console.WriteLine($"Arquivo de configuração criado em {configPath}.");
    }

    static void InstallService(string executablePath)
    {
        ExecuteCommand(executablePath, "install");
        Console.WriteLine("Serviço instalado com sucesso.");
    }

    static void StartService(string executablePath)
    {
        ExecuteCommand(executablePath, "start");
        Console.WriteLine("Serviço iniciado com sucesso.");
    }

    static void ExecuteCommand(string executablePath, string argument)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = argument,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        if (!string.IsNullOrEmpty(output))
            Console.WriteLine(output);

        if (!string.IsNullOrEmpty(error))
            throw new Exception(error);
    }
}
