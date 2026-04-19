using QuestionBank.Domain;
using QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Content.Questions.Constants;
using QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Reference;

namespace QuestionBank.Infrastructure.Implementation.DataAccess.Seeding.Content.Questions;

public class CSharpTheoryQuestionsSeed : QuestionSeedBase
{
    protected override IReadOnlyCollection<QuestionDefinition> Questions =>
    [
        new(
            Id: QuestionIds.CSharpClassVsStruct,
            Title: "Чем class отличается от struct в C#?",
            Text: "Объясните различия между class и struct в C#. Укажите различия по семантике хранения, копированию и типичным сценариям использования.",
            Type: QuestionType.Theory,
            ReferenceSolution: """
                class — ссылочный тип, а struct — значимый тип.
                Экземпляр class обычно передается по ссылке на объект, а struct копируется целиком при присваивании и передаче в метод без ref/out/in.
                struct подходит для небольших неизменяемых объектов данных, например координат или значений.
                class удобнее использовать для объектов с поведением, наследованием и более сложным жизненным циклом.
                """,
            CompetencyId: CompetencyIds.CSharpCore,
            GradeId: GradeIds.Junior,
            TechnologyId: TechnologyIds.CSharp
        ),

        new(
            Id: QuestionIds.AspNetCoreMiddleware,
            Title: "Что такое middleware в ASP.NET Core?",
            Text: "Объясните, что такое middleware в ASP.NET Core и какую роль middleware выполняет в обработке HTTP-запроса.",
            Type: QuestionType.Theory,
            ReferenceSolution: """
                Middleware — это компонент конвейера обработки HTTP-запроса.
                Каждый middleware может выполнить логику до передачи запроса дальше и после получения ответа от следующего компонента.
                Через middleware обычно реализуют логирование, обработку ошибок, аутентификацию, авторизацию и маршрутизацию.
                Порядок подключения middleware важен, потому что он определяет, как именно пройдет запрос по pipeline.
                """,
            CompetencyId: CompetencyIds.AspNetCoreBasics,
            GradeId: GradeIds.Junior,
            TechnologyId: TechnologyIds.AspNetCore
        ),

        new(
            Id: QuestionIds.AspNetCoreServiceLifetimes,
            Title: "Чем отличаются Transient, Scoped и Singleton?",
            Text: "Объясните различия между временами жизни сервисов Transient, Scoped и Singleton в ASP.NET Core DI-контейнере.",
            Type: QuestionType.Theory,
            ReferenceSolution: """
                Transient создается заново при каждом запросе зависимости.
                Scoped создается один раз в рамках HTTP-запроса и переиспользуется внутри этого запроса.
                Singleton создается один раз на все приложение и живет до его остановки.
                Scoped часто используют для DbContext, Singleton — для потокобезопасных сервисов без состояния запроса, а Transient — для легких вспомогательных компонентов.
                """,
            CompetencyId: CompetencyIds.AspNetCoreBasics,
            GradeId: GradeIds.Junior,
            TechnologyId: TechnologyIds.AspNetCore
        ),

        new(
            Id: QuestionIds.EfCoreTrackingVsNoTracking,
            Title: "Что делает AsNoTracking в EF Core?",
            Text: "Объясните, что означает AsNoTracking в Entity Framework Core и в каких случаях его стоит использовать.",
            Type: QuestionType.Theory,
            ReferenceSolution: """
                По умолчанию EF Core отслеживает загруженные сущности, чтобы затем уметь сохранить их изменения.
                AsNoTracking отключает это отслеживание для конкретного запроса.
                Такой режим полезен для read-only запросов, когда данные нужно только прочитать и не планируется их изменять через текущий DbContext.
                Это может уменьшить накладные расходы по памяти и немного ускорить чтение.
                """,
            CompetencyId: CompetencyIds.EfCoreBasics,
            GradeId: GradeIds.Junior,
            TechnologyId: TechnologyIds.EfCore
        ),

        new(
            Id: QuestionIds.EfCoreInclude,
            Title: "Зачем в EF Core использовать Include?",
            Text: "Объясните, зачем в Entity Framework Core используется Include и какую проблему он помогает решить при загрузке связанных данных.",
            Type: QuestionType.Theory,
            ReferenceSolution: """
                Include используют для жадной загрузки связанных сущностей вместе с основной.
                Например, можно загрузить список заказов сразу вместе с их позициями.
                Это помогает избежать дополнительных запросов к базе и делает поведение загрузки данных явным.
                Include особенно полезен, когда связанные данные точно понадобятся в текущем сценарии.
                """,
            CompetencyId: CompetencyIds.EfCoreBasics,
            GradeId: GradeIds.Junior,
            TechnologyId: TechnologyIds.EfCore
        ),

        new(
            Id: QuestionIds.CSharpIEnumerableVsList,
            Title: "В чем разница между IEnumerable<T> и List<T>?",
            Text: "Объясните различие между IEnumerable<T> и List<T>. Когда достаточно возвращать IEnumerable<T>, а когда нужен именно List<T>?",
            Type: QuestionType.Theory,
            ReferenceSolution: """
                IEnumerable<T> описывает последовательность, по которой можно пройти в foreach, но не гарантирует наличие индексации и методов изменения.
                List<T> — конкретная коллекция с хранением элементов в памяти, доступом по индексу и возможностью добавлять и удалять элементы.
                IEnumerable<T> удобно использовать в сигнатурах, когда вызывающей стороне достаточно только читать последовательность.
                List<T> нужен, когда требуется конкретная изменяемая коллекция или быстрый доступ по индексу.
                """,
            CompetencyId: CompetencyIds.DataStructures,
            GradeId: GradeIds.Junior,
            TechnologyId: TechnologyIds.CSharp
        ),

        new(
            Id: QuestionIds.BackendValidationEdgeCases,
            Title: "Почему на backend важно валидировать входные данные и учитывать крайние случаи?",
            Text: "Объясните, почему в backend-разработке важно проверять входные данные и учитывать крайние случаи. Приведите примеры.",
            Type: QuestionType.Theory,
            ReferenceSolution: """
                На backend нельзя доверять входным данным без проверки, потому что они приходят от внешнего клиента.
                Нужно учитывать null, пустые строки, отрицательные значения, слишком большие payload, отсутствие связанных сущностей и некорректный формат данных.
                Валидация помогает вернуть понятную ошибку клиенту и предотвратить падения приложения или запись некорректных данных в базу.
                Учет крайних случаев повышает надежность API и упрощает дальнейшую поддержку системы.
                """,
            CompetencyId: CompetencyIds.TestingDebugging,
            GradeId: GradeIds.Junior,
            TechnologyId: null
        )
    ];
}
