using FluentMigrator;

namespace Route256.Week5.Workshop.PriceCalculator.Dal.Migrations;

[Migration(20240413000000, TransactionBehavior.None)]
public class AddModifiedAtField : Migration
{
    public override void Up()
    {
        const string sql = @"
DO $$
    BEGIN
        alter table task_comments
            add column if not exists modified_at timestamp with time zone null;
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
            drop column modified_at;
    END
$$;";
        
        Execute.Sql(sql);
    }
}