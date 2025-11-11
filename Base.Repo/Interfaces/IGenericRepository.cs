using Base.Repo.Specifications;
using Base.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Repo.Interfaces
{
    public interface IGenericRepository<T> where T : BaseEntity
    {

        // ------------------- عمليات الكتابة (Async) --------------------
        Task<T> AddAsync(T entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);

        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);

        // -------------------- عمليات القراءة -----------------------
        Task<T> GetByIdAsync(string id);
        // 🟢 وقائي: List للإشارة إلى مجموعة (Read-only)
        Task<IReadOnlyList<T>> ListAllAsync();

        // ------------------- عمليات المواصفات (Spec) ------------------
        // 🟢 وقائي: List للإشارة إلى مجموعة مع شرط
        Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec);

        Task<T> GetEntityWithSpecAsync(ISpecification<T> spec);

        // 🟢 وقائي: CountAsync للتأكد من أنها غير متزامنة
        Task<int> CountAsync(ISpecification<T> spec);
    }
}
