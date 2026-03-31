using QuestionBank.Domain;
using QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Content.Questions.Constants;
using QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Reference;

namespace QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Content.Questions;

public class PythonTheoryQuestionsSeed : QuestionSeedBase
{
    protected override IReadOnlyCollection<QuestionDefinition> Questions =>
    [
        new(
            Id: QuestionIds.PythonListVsTuple,
            Title: "Чем list отличается от tuple в Python?",
            Text: "Объясните различия между list и tuple в Python. Укажите различия по изменяемости, производительности и типичным сценариям использования.",
            Type: QuestionType.Theory,
            ReferenceSolution: """
                list — изменяемая последовательность, tuple — неизменяемая.
                list удобно использовать, когда набор элементов должен изменяться: добавление, удаление, обновление.
                tuple подходит для фиксированных наборов данных, которые не должны изменяться после создания.
                tuple обычно немного экономнее по памяти и может использоваться как ключ словаря, если содержит только хешируемые значения.
                """,
            CompetencyId: CompetencyIds.PythonCore,
            GradeId: GradeIds.Middle,
            TechnologyId: TechnologyIds.Python
        ),

        new(
            Id: QuestionIds.PythonIsVsEquals,
            Title: "В чем разница между is и == в Python?",
            Text: "Объясните различие между операторами is и == в Python. Приведите пример, когда результат их работы будет отличаться.",
            Type: QuestionType.Theory,
            ReferenceSolution: """
                Оператор == сравнивает значения объектов, а оператор is проверяет, ссылаются ли переменные на один и тот же объект в памяти.
                Например, два разных списка с одинаковыми элементами будут равны через ==, но is вернет False, так как это разные объекты.
                is обычно используют для проверки на None: value is None.
                """,
            CompetencyId: CompetencyIds.PythonCore,
            GradeId: GradeIds.Middle,
            TechnologyId: TechnologyIds.Python
        ),

        new(
            Id: QuestionIds.PythonDictVsSet,
            Title: "В каких случаях стоит использовать dict, а в каких set?",
            Text: "Объясните, в каких случаях для решения задачи следует использовать словарь, а в каких — множество. Приведите примеры.",
            Type: QuestionType.Theory,
            ReferenceSolution: """
                dict используют, когда нужно хранить пары ключ-значение и быстро получать значение по ключу.
                set используют, когда нужно хранить уникальные элементы и быстро проверять принадлежность.
                Например, dict подходит для подсчета частот элементов, а set — для удаления дублей или проверки, встречался ли элемент ранее.
                Обе структуры обычно дают близкую к O(1) среднюю сложность доступа благодаря хешированию.
                """,
            CompetencyId: CompetencyIds.DataStructures,
            GradeId: GradeIds.Middle,
            TechnologyId: TechnologyIds.Python
        ),

        new(
            Id: QuestionIds.PythonComplexity,
            Title: "Что такое временная и пространственная сложность алгоритма?",
            Text: "Объясните, что означает временная и пространственная сложность алгоритма. Приведите примеры сложностей O(1), O(n) и O(log n).",
            Type: QuestionType.Theory,
            ReferenceSolution: """
                Временная сложность показывает, как растет количество операций при увеличении размера входных данных.
                Пространственная сложность показывает, как растет дополнительная используемая память.
                O(1) — доступ к элементу массива по индексу.
                O(n) — линейный проход по массиву.
                O(log n) — бинарный поиск в отсортированном массиве.
                Асимптотическая оценка позволяет сравнивать эффективность алгоритмов на больших объемах данных.
                """,
            CompetencyId: CompetencyIds.AlgorithmsBasic,
            GradeId: GradeIds.Middle,
            TechnologyId: TechnologyIds.Python
        ),

        new(
            Id: QuestionIds.PythonBinarySearch,
            Title: "При каких условиях можно применять бинарный поиск?",
            Text: "Опишите условия корректного применения бинарного поиска. Объясните, почему этот алгоритм работает быстрее линейного поиска.",
            Type: QuestionType.Theory,
            ReferenceSolution: """
                Бинарный поиск применяется, когда данные упорядочены по возрастанию или убыванию и к ним есть быстрый доступ по индексу.
                На каждом шаге алгоритм сравнивает искомое значение с серединой диапазона и отбрасывает половину элементов.
                Поэтому сложность бинарного поиска равна O(log n), тогда как линейный поиск в худшем случае требует O(n).
                Если данные не отсортированы, бинарный поиск использовать нельзя без предварительной сортировки.
                """,
            CompetencyId: CompetencyIds.AlgorithmsBasic,
            GradeId: GradeIds.Middle,
            TechnologyId: TechnologyIds.Python
        ),

        new(
            Id: QuestionIds.PythonMutableDefaultArgument,
            Title: "Почему изменяемые значения по умолчанию в аргументах функции могут быть проблемой?",
            Text: "Объясните, почему использование изменяемых значений по умолчанию в аргументах функции может приводить к ошибкам. Покажите правильный подход.",
            Type: QuestionType.Theory,
            ReferenceSolution: """
                Значение аргумента по умолчанию вычисляется один раз в момент определения функции, а не при каждом вызове.
                Поэтому если использовать, например, список как значение по умолчанию и изменять его внутри функции, состояние будет сохраняться между вызовами.
                Это может приводить к трудноуловимым ошибкам.
                Правильный подход — использовать None по умолчанию, а внутри функции создавать новый объект:
                if items is None: items = [].
                """,
            CompetencyId: CompetencyIds.PythonCore,
            GradeId: GradeIds.Middle,
            TechnologyId: TechnologyIds.Python
        ),

        new(
            Id: QuestionIds.PythonTestingEdgeCases,
            Title: "Зачем при тестировании учитывать граничные случаи?",
            Text: "Объясните, почему при проверке решения важно учитывать граничные случаи. Приведите примеры таких случаев для задач на массивы или строки.",
            Type: QuestionType.Theory,
            ReferenceSolution: """
                Граничные случаи помогают выявить ошибки, которые не проявляются на обычных тестах.
                Это могут быть пустые массивы, массивы из одного элемента, очень большие входные данные, повторяющиеся значения, отрицательные числа, пустые строки.
                Для строк также важно учитывать разный регистр, специальные символы и отсутствие символов вообще.
                Проверка edge cases повышает надежность решения и помогает убедиться, что алгоритм корректно работает во всех допустимых сценариях.
                """,
            CompetencyId: CompetencyIds.TestingDebugging,
            GradeId: GradeIds.Middle,
            TechnologyId: TechnologyIds.Python
        )
    ];
}