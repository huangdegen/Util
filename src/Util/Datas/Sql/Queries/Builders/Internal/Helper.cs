﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Util.Datas.Queries;
using Util.Datas.Sql.Queries.Builders.Abstractions;
using Util.Datas.Sql.Queries.Builders.Conditions;
using Util.Datas.Sql.Queries.Builders.Core;
using Util.Helpers;

namespace Util.Datas.Sql.Queries.Builders.Internal {
    /// <summary>
    /// Sql生成器辅助操作
    /// </summary>
    internal class Helper {
        /// <summary>
        /// 方言
        /// </summary>
        private readonly IDialect _dialect;
        /// <summary>
        /// 实体解析器
        /// </summary>
        private readonly IEntityResolver _resolver;
        /// <summary>
        /// 实体别名注册器
        /// </summary>
        private readonly IEntityAliasRegister _register;
        /// <summary>
        /// 参数管理器
        /// </summary>
        private readonly IParameterManager _parameterManager;

        /// <summary>
        /// 初始化Sql生成器辅助操作
        /// </summary>
        /// <param name="dialect">方言</param>
        /// <param name="resolver">实体解析器</param>
        /// <param name="register">实体别名注册器</param>
        /// <param name="parameterManager">参数管理器</param>
        public Helper( IDialect dialect, IEntityResolver resolver, IEntityAliasRegister register, IParameterManager parameterManager ) {
            _dialect = dialect;
            _resolver = resolver;
            _register = register;
            _parameterManager = parameterManager;
        }

        /// <summary>
        /// 获取处理后的列名
        /// </summary>
        /// <param name="expression">表达式</param>
        /// <param name="type">实体类型</param>
        public string GetColumn( Expression expression, Type type ) {
            return GetColumn( _resolver.GetColumn( expression, type ), type );
        }

        /// <summary>
        /// 获取处理后的列名
        /// </summary>
        /// <param name="expression">列名表达式</param>
        public string GetColumn<TEntity>( Expression<Func<TEntity, object>> expression ) {
            return GetColumn( _resolver.GetColumn( expression ), typeof( TEntity ) );
        }

        /// <summary>
        /// 获取处理后的列名
        /// </summary>
        /// <param name="column">列名</param>
        /// <param name="type">实体类型</param>
        public string GetColumn( string column, Type type ) {
            return new SqlItem( column, _register.GetAlias( type ) ).ToSql( _dialect );
        }

        /// <summary>
        /// 获取处理后的列名
        /// </summary>
        /// <param name="column">列名</param>
        public string GetColumn( string column ) {
            return new SqlItem( column ).ToSql( _dialect );
        }

        /// <summary>
        /// 创建查询条件并添加参数
        /// </summary>
        /// <param name="expression">列名</param>
        /// <param name="type">实体类型</param>
        public ICondition CreateCondition( Expression expression, Type type ) {
            return CreateCondition( GetColumn( expression, type ), Lambda.GetValue( expression ), Lambda.GetOperator( expression ).SafeValue() );
        }

        /// <summary>
        /// 创建查询条件并添加参数
        /// </summary>
        /// <param name="column">列名</param>
        /// <param name="value">值</param>
        /// <param name="operator">运算符</param>
        public ICondition CreateCondition( string column, object value, Operator @operator ) {
            if( string.IsNullOrWhiteSpace( column ) )
                throw new ArgumentNullException( nameof( column ) );
            column = GetColumn( column );
            if( @operator == Operator.Contains && value != null && Reflection.IsCollection( value.GetType() ) )
                return CreateInCondition( column, value as IEnumerable );
            var paramName = GenerateParamName( value, @operator );
            _parameterManager.Add( paramName, value, @operator );
            return SqlConditionFactory.Create( column, paramName, @operator );
        }

        /// <summary>
        /// 创建In条件
        /// </summary>
        private ICondition CreateInCondition( string column, IEnumerable values ) {
            if( values == null )
                return NullCondition.Instance;
            var paramNames = new List<string>();
            foreach( var value in values ) {
                var name = _parameterManager.GenerateName();
                paramNames.Add( name );
                _parameterManager.Add( name, value );
            }
            return new InCondition( column, paramNames );
        }

        /// <summary>
        /// 获取参数名
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="operator">运算符</param>
        public string GenerateParamName( object value, Operator @operator ) {
            var result = _parameterManager.GenerateName();
            if( value != null )
                return result;
            if( @operator == Operator.Equal || @operator == Operator.NotEqual )
                return null;
            return result;
        }
    }
}
