using System;
using System.Collections.Generic;

namespace CompraProgramada.Domain.Entities
{
    public enum StatusOrdem
    {
        Pendente,
        Executada,
        Cancelada
    }

    public class OrdemCompra
    {
        public long Id { get; private set; }
        public long ClienteId { get; private set; }
        public Cliente Cliente { get; private set; } = null!;
        public DateTime DataCriacao { get; private set; }
        public DateTime? DataConclusao { get; private set; }
        public StatusOrdem Status { get; private set; }
        public decimal ValorTotal { get; private set; }
        public decimal ValorCarteiraNoMomento { get; set; } // valor total da carteira do cliente no instante da compra
        public List<OrdemCompraItem> Itens { get; private set; } = new List<OrdemCompraItem>();

        private OrdemCompra() { }

        public static OrdemCompra Criar(long clienteId, IEnumerable<OrdemCompraItem> itens)
        {
            if (clienteId <= 0)
                throw new ArgumentException("ClienteId deve ser positivo.");
            if (itens == null)
                throw new ArgumentNullException(nameof(itens));

            var lista = itens.ToList();
            if (lista.Count == 0)
                throw new ArgumentException("A ordem deve conter ao menos um item.");

            var ordem = new OrdemCompra
            {
                ClienteId = clienteId,
                DataCriacao = DateTime.UtcNow,
                Status = StatusOrdem.Pendente
            };
            foreach (var item in lista)
            {
                ordem.AdicionarItem(item);
            }
            ordem.ValorTotal = lista.Sum(i => i.ValorItem);
            return ordem;
        }

        public void AdicionarItem(OrdemCompraItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            item.OrdemCompra = this;
            // if this order already has a key, propagate to item
            if (Id != 0)
            {
                item.OrdemCompraId = Id;
            }
            Itens.Add(item);
            ValorTotal += item.ValorItem;
        }

        public void MarcarExecutada()
        {
            Status = StatusOrdem.Executada;
            DataConclusao = DateTime.UtcNow;
        }

        public void MarcarCancelada()
        {
            Status = StatusOrdem.Cancelada;
            DataConclusao = DateTime.UtcNow;
        }
    }

    public class OrdemCompraItem
    {
        public long Id { get; private set; }
        public long OrdemCompraId { get; internal set; }
        public OrdemCompra? OrdemCompra { get; internal set; }
        public string Ticker { get; private set; } = string.Empty;
        public decimal Quantidade { get; private set; }
        public decimal PrecoUnitario { get; private set; }
        public decimal ValorItem { get; private set; }
        public bool Fracionario { get; private set; }

        private OrdemCompraItem() { }

        public static OrdemCompraItem Criar(string ticker, decimal quantidade, decimal precoUnitario, bool fracionario = false)
        {
            if (string.IsNullOrWhiteSpace(ticker))
                throw new ArgumentException("Ticker obrigatório.");
            if (quantidade <= 0)
                throw new ArgumentException("Quantidade deve ser maior que zero.");
            if (precoUnitario <= 0)
                throw new ArgumentException("Preço unitário deve ser maior que zero.");

            return new OrdemCompraItem
            {
                Ticker = ticker,
                Quantidade = quantidade,
                PrecoUnitario = precoUnitario,
                ValorItem = quantidade * precoUnitario,
                Fracionario = fracionario
            };
        }
    }
}