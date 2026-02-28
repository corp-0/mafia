using Mafia.Core.Text;

namespace Mafia.Core.Ecs.Components.State;

public enum ExpenseCategory
{
    Food,
    Housing,
    Education,
    Medical,
    Clothing,
    Entertainment
}

public record struct Expense(ExpenseCategory Category, int Amount);

public record struct ExpenseLabel(Localizable Label);
