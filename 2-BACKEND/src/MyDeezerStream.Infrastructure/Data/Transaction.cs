using Microsoft.EntityFrameworkCore.Storage;
using MyDeezerStream.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDeezerStream.Infrastructure.Data
{
    public class Transaction : ITransaction
    {
        private readonly IDbContextTransaction _efTransaction;

        public Transaction(IDbContextTransaction efTransaction)
        {
            _efTransaction = efTransaction;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
            => await _efTransaction.CommitAsync(cancellationToken);

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
            => await _efTransaction.RollbackAsync(cancellationToken);

        public async ValueTask DisposeAsync()
            => await _efTransaction.DisposeAsync();
    }
}
