using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SagaDb.Models
{
    public enum AuthorType
    {
        Unknown = 0,
        Author = 1,
        Editor = 2,
        Translator = 3,
        Foreword = 4,
        Contributor = 5,
        Illustrator = 6,
        Narrator = 7
    }
}
