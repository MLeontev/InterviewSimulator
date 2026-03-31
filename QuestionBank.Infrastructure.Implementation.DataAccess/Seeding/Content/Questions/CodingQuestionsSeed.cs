using QuestionBank.Domain;
using QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Content.Questions.Constants;
using QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Reference;

namespace QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Content.Questions;

public class CodingQuestionsSeed : QuestionSeedBase
{
    protected override IReadOnlyCollection<QuestionDefinition> Questions =>
    [
        new(
            Id: QuestionIds.TwoSum,
            Title: "Two Sum",
            Text: """
                Дан массив целых чисел nums и целое число target.
                Необходимо вернуть индексы двух чисел, сумма которых равна target.
                Гарантируется, что существует ровно одно решение, и один и тот же элемент нельзя использовать дважды.
                Ответ можно вернуть в любом порядке.
                """,
            Type: QuestionType.Coding,
            ReferenceSolution: """
                Оптимальное решение использует хеш-таблицу.
                При обходе массива для текущего элемента x вычисляется complement = target - x.
                Если complement уже встречался ранее, возвращаются индексы текущего элемента и complement.
                Если нет, текущий элемент сохраняется в словарь/хеш-таблицу.
                Сложность решения: O(n) по времени и O(n) по памяти.
                """,
            CompetencyId: CompetencyIds.CodingProblemSolving,
            GradeId: GradeIds.Middle,
            TechnologyId: null,
            LanguageLimits:
            [
                new LanguageLimitDefinition(
                    LanguageId: TechnologyIds.Python,
                    TimeLimitMs: 2000,
                    MemoryLimitMb: 128)
            ],
            TestCases:
            [
                new TestCaseDefinition(
                    Input: """
                        [2,7,11,15]
                        9
                        """,
                    ExpectedOutput: "[0,1]",
                    IsHidden: false,
                    OrderIndex: 1),

                new TestCaseDefinition(
                    Input: """
                        [3,2,4]
                        6
                        """,
                    ExpectedOutput: "[1,2]",
                    IsHidden: false,
                    OrderIndex: 2),

                new TestCaseDefinition(
                    Input: """
                        [3,3]
                        6
                        """,
                    ExpectedOutput: "[0,1]",
                    IsHidden: true,
                    OrderIndex: 3)
            ]
        ),

        new(
            Id: QuestionIds.ValidAnagram,
            Title: "Проверка анаграммы",
            Text: """
                Даны две строки s и t.
                Необходимо определить, являются ли они анаграммами друг друга.
                Анаграммы содержат одинаковые символы в одинаковом количестве, но могут отличаться порядком символов.
                Верните true, если строки являются анаграммами, иначе false.
                """,
            Type: QuestionType.Coding,
            ReferenceSolution: """
                Сначала проверяется равенство длин строк.
                Если длины различаются, строки не могут быть анаграммами.
                Далее можно использовать хеш-таблицу для подсчета частот символов или сравнить отсортированные строки.
                Более эффективный подход — подсчет частот.
                Сложность: O(n) по времени и O(n) по памяти.
                """,
            CompetencyId: CompetencyIds.CodingProblemSolving,
            GradeId: GradeIds.Middle,
            TechnologyId: null,
            LanguageLimits:
            [
                new LanguageLimitDefinition(
                    LanguageId: TechnologyIds.Python,
                    TimeLimitMs: 2000,
                    MemoryLimitMb: 128)
            ],
            TestCases:
            [
                new TestCaseDefinition(
                    Input: """
                        anagram
                        nagaram
                        """,
                    ExpectedOutput: "true",
                    IsHidden: false,
                    OrderIndex: 1),

                new TestCaseDefinition(
                    Input: """
                        rat
                        car
                        """,
                    ExpectedOutput: "false",
                    IsHidden: false,
                    OrderIndex: 2),

                new TestCaseDefinition(
                    Input: """
                        aacc
                        ccac
                        """,
                    ExpectedOutput: "false",
                    IsHidden: true,
                    OrderIndex: 3)
            ]
        ),

        new(
            Id: QuestionIds.FirstUniqueCharacter,
            Title: "Первый неповторяющийся символ",
            Text: """
                Дана строка s.
                Необходимо вернуть индекс первого символа, который встречается в строке ровно один раз.
                Если такого символа нет, вернуть -1.
                """,
            Type: QuestionType.Coding,
            ReferenceSolution: """
                Сначала подсчитывается количество вхождений каждого символа.
                Затем выполняется второй проход по строке слева направо.
                Как только найден символ с частотой 1, возвращается его индекс.
                Если таких символов нет, возвращается -1.
                Сложность: O(n) по времени и O(n) по памяти.
                """,
            CompetencyId: CompetencyIds.CodingProblemSolving,
            GradeId: GradeIds.Middle,
            TechnologyId: null,
            LanguageLimits:
            [
                new LanguageLimitDefinition(
                    LanguageId: TechnologyIds.Python,
                    TimeLimitMs: 2000,
                    MemoryLimitMb: 128)
            ],
            TestCases:
            [
                new TestCaseDefinition(
                    Input: """
                        leetcode
                        """,
                    ExpectedOutput: "0",
                    IsHidden: false,
                    OrderIndex: 1),

                new TestCaseDefinition(
                    Input: """
                        loveleetcode
                        """,
                    ExpectedOutput: "2",
                    IsHidden: false,
                    OrderIndex: 2),

                new TestCaseDefinition(
                    Input: """
                        aabb
                        """,
                    ExpectedOutput: "-1",
                    IsHidden: true,
                    OrderIndex: 3)
            ]
        )
    ];
}