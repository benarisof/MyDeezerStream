using MyDeezerStream.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDeezerStream.Application.Interfaces
{
    public interface IUserContext
    {
        string GetCurrentAuth0UserId();
    }
}
