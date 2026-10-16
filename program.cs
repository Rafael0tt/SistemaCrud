using System; // Traz a CAIXA DE FERRAMENTAS básica do C#
using System.Collections.Generic; // Caixa para guardar LISTAS de dados
using System.Net; // Cria o SERVIDOR web local
using System.Net.WebSockets; // O sistema de TEMPO REAL, tipo chat
using System.Text; // Traduz TEXTO para bytes que o PC entende
using System.Text.Json; // Converte os dados em formato JSON
using System.Threading; // Controla o FLUXO das tarefas
using System.Threading.Tasks; // Roda TAREFAS em segundo plano sem travar

public class Produto // O MOLDE oficial de como cada produto é feito
{ // Abre a chave da classe Produto
    public int Id { get; set; } // O ID único do produto
    public string Nome { get; set; } // O NOME do produto
    public decimal Preco { get; set; } // O PREÇO do produto
    public int Stock { get; set; } // A QUANTIDADE guardada no armazém
} // Fecha a chave da classe Produto

class Program // O QUARTEL-GENERAL onde o programa roda
{ // Abre a chave da classe Program
    private static List<Produto> produtos = new List<Produto>(); // A LISTA na memória para guardar os produtos
    private static int proximoId = 1; // O CONTADOR automático que dá um ID único

    static async Task Main(string[] args) // O botão de PLAY, onde o código começa
    { // Abre a chave da função Main
        var httpListener = new HttpListener(); // Prepara o SERVIDOR web local
        httpListener.Prefixes.Add("http://localhost:5000/"); // Define o ENDEREÇO exato do servidor
        httpListener.Start(); // LIGA o servidor, vapo!
        Console.WriteLine("Servidor WebSocket do Painel de Produtos a correr em http://localhost:5000/"); // Manda a call no terminal avisando que ligou

        while (true) // LOOP INFINITO, o servidor nunca dorme, tá sempre on
        { // Abre a chave do while do Main
            var context = await httpListener.GetContextAsync(); // Fica à espera até alguém tentar entrar
            if (context.Request.IsWebSocketRequest) // Vê se o cliente quer ligação WEBSOCKET
            { // Abre a chave do if (é websocket)
                ProcessWebSocket(context); // Se for, aceita e libera a CONVERSA em tempo real
            } // Fecha a chave do if (é websocket)
            else // Se não for websocket...
            { // Abre a chave do else
                context.Response.StatusCode = 400; // Manda um ERRO 400 de rejeitado
                context.Response.Close(); // Fecha a conexão
            } // Fecha a chave do else
        } // Fecha a chave do while do Main
    } // Fecha a chave da função Main

    private static async void ProcessWebSocket(HttpListenerContext context) // A função que GERE a ligação de cada cliente
    { // Abre a chave da função ProcessWebSocket
        var wsContext = await context.AcceptWebSocketAsync(null); // Aceita a LIGAÇÃO do WebSocket
        var socket = wsContext.WebSocket; // Guarda o canal de comunicação numa variável
        Console.WriteLine("Cliente ligado ao painel!"); // Avisa no terminal que chegou visita

        await EnviarListaProdutos(socket); // Envia a LISTA atualizada logo de cara

        var buffer = new byte[1024 * 4]; // A MEMÓRIA (buffer) para guardar o que vier do navegador
        while (socket.State == WebSocketState.Open) // Fica rodando enquanto a aba estiver ABERTA
        { // Abre a chave do while do WebSocket
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None); // Ouve o que o navegador mandou
            if (result.MessageType == WebSocketMessageType.Close) // Se o cliente FECHOU a aba
            { // Abre a chave do if (fechar)
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Fechado", CancellationToken.None); // Encerra o WebSocket
            } // Fecha a chave do if (fechar)
            else // Se mandou uma mensagem normal...
            { // Abre a chave do else
                string mensagemJson = Encoding.UTF8.GetString(buffer, 0, result.Count); // Transforma os bytes em TEXTO JSON
                await TratarMensagemCrud(socket, mensagemJson); // Manda para o cérebro do CRUD decidir o que fazer
            } // Fecha a chave do else
        } // Fecha a chave do while do WebSocket
    } // Fecha a chave da função ProcessWebSocket

    private static async Task TratarMensagemCrud(WebSocket socket, string mensagemJson) // O CÉREBRO que decide se cria ou apaga o produto
    { // Abre a chave da função TratarMensagemCrud
        using var doc = JsonDocument.Parse(mensagemJson); // Lê o pacote JSON que chegou
        var root = doc.RootElement; // Pega a raiz do JSON
        string acao = root.GetProperty("acao").GetString(); // Descobre qual é a AÇÃO (criar ou apagar)

        switch (acao) // Escolhe o caminho dependendo da ação
        { // Abre a chave do switch
            case "criar": // Se a ordem for CRIAR um produto novo:
                var novoProduto = new Produto // Monta o objeto com os dados que vieram
                { // Abre a chave do objeto Produto
                    Id = proximoId++, // Atribui o ID automático e soma mais um
                    Nome = root.GetProperty("nome").GetString(), // Pega o NOME digitado
                    Preco = root.GetProperty("preco").GetDecimal(), // Pega o PREÇO
                    Stock = root.GetProperty("stock").GetInt32() // Pega a QUANTIDADE em stock
                }; // Fecha a chave do objeto Produto
                produtos.Add(novoProduto); // Joga o produto novo dentro da LISTA
                break; // Fim da ação criar

            case "apagar": // Se a ordem for APAGAR:
                int idApagar = root.GetProperty("id").GetInt32(); // Descobre o ID que deve ser removido
                produtos.RemoveAll(p => p.Id == idApagar); // Tira o produto da lista
                break; // Fim da ação apagar
        } // Fecha a chave do switch

        await EnviarListaProdutos(socket); // Atualiza a LISTA e manda de volta para o cliente
    } // Fecha a chave da função TratarMensagemCrud

    private static async Task EnviarListaProdutos(WebSocket socket) // EMPACOTA a lista de produtos e envia para o site
    { // Abre a chave da função EnviarListaProdutos
        string json = JsonSerializer.Serialize(new { acao = "listar", dados = produtos }); // Transforma a lista num texto JSON
        var bytes = Encoding.UTF8.GetBytes(json); // Converte o JSON em bytes
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None); // Envia para o navegador
    } // Fecha a chave da função EnviarListaProdutos
} // Fecha a chave da classe Program