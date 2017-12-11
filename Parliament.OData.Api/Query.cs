using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace WebApplication1
{
    //public class Query<T> : IOrderedQueryable<T>
    //{
    //    public DbQueryProvider Provider { get; private set; }
    //    public Expression Expression { get; private set; }

    //    public Query()
    //    {
    //        Provider = new DbQueryProvider();
    //        Expression = Expression.Constant(this);
    //    }

    //    public Query(DbQueryProvider provider, Expression expression)
    //    {
    //        if (provider == null)
    //            throw new ArgumentNullException("provider");

    //        if (expression == null)
    //            throw new ArgumentNullException("expression");

    //        if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
    //          throw new ArgumentOutOfRangeException("expression");

    //        Provider = provider;
    //        Expression = expression;
    //    }

    //    public Type ElementType
    //    {
    //        get
    //        {
    //            return typeof(T);
    //        }
    //    }

    //    public IEnumerator<T> GetEnumerator()
    //    {
    //        return ((IEnumerable<T>)Provider.Execute(Expression)).GetEnumerator();
    //    }

    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        return ((IEnumerable)Provider.Execute(Expression)).GetEnumerator();
    //    }          

    //}
}