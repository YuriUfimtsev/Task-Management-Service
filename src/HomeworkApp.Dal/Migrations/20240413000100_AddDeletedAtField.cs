using FluentMigrator;

namespace Route256.Week5.Workshop.PriceCalculator.Dal.Migrations;

[Migration(20240413000100, TransactionBehavior.None)]
public class AddDeletedAtField : Migration
{
    public override void Up()
    {
        const string sql = @"
DO $$
    BEGIN
        alter table task_comments
            add column if not exists deleted_at timestamp with time zone null;
    END
$$";
        
        Execute.Sql(sql);
    }
    
    public override void Down()
    {
        const string sql = @"
DO $$
    BEGIN
        alter table task_comments
            drop column deleted_at;
    END
$$;";
        
        Execute.Sql(sql);
    }
}