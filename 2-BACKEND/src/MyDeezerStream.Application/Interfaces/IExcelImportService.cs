using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDeezerStream.Application.Interfaces
{
    public interface IExcelImportService
    {
        Task<int> ImportFromExcelAsync(System.IO.Stream excelStream, int userId, CancellationToken cancellationToken = default);
    }
}
