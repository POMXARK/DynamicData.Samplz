using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using DynamicData.Samplz.Infrastructure;

namespace DynamicData.Samplz.Examples
{
    public class FilterObservableViewModel
    {

        private readonly ReadOnlyObservableCollection<FootballPlayer> _availablePlayers;


        public ReadOnlyObservableCollection<FootballPlayer> AvailablePlayers => _availablePlayers;

        public FilterObservableViewModel()
        {
            //Load available players
            CreateFootballerList().Connect()
                .FilterOnObservable(person => person.IncludedChanged, included => !included)
                .ObserveOnDispatcher()
                .Bind(out _availablePlayers)
                .Subscribe();
        }

        private ISourceList<FootballPlayer> CreateFootballerList()
        {
            var people = new SourceList<FootballPlayer>();
            people.AddRange(new[]
            {
                new FootballPlayer("Hennessey"),
                new FootballPlayer("Chester"),
                new FootballPlayer("Williams"),
            });
            return people;
        }
    }

    public class FootballPlayer
    {
        public string Name { get;  }
        public ICommand IncludeCommand { get; }
        public IObservable<bool> IncludedChanged { get; }

        public FootballPlayer(string name )
        {
            var includeChanged = new BehaviorSubject<bool>(false);

            Name = name;
            IncludeCommand = new Command(() => includeChanged.OnNext(true));
            IncludedChanged = includeChanged.AsObservable();
        }
    }

    public static class DynamicDataEx
    {
        // Собственный реактивный метод
        public static IObservable<IChangeSet<TObject>> FilterOnObservable<TObject, TValue>(this IObservable<IChangeSet<TObject>> source, 
            Func<TObject, IObservable<TValue>> observableSelector,
            Func<TValue, bool> predicate)
        {
            return Observable.Create<IChangeSet<TObject>>(observer =>
            {

                //create a local list to store matching values
                //создаем локальный список для хранения совпадающих значений
                var resultList = new SourceList<TObject>();

                //monitor whether the observable has changed and amend local list accordingly
                //отслеживаем, изменился ли наблюдаемый объект, и вносим соответствующие изменения в локальный список
                source.SubscribeMany(item =>
                {
                    return observableSelector(item)
                        .Subscribe(value =>
                        {

                            var isMatched = predicate(value);
                            if (isMatched)
                            {
                                //prevent duplicates with contains check - otherwise use a source cache
                                //предотвратить дубликаты с проверкой содержимого - в противном случае используйте исходный кеш
                                if (!resultList.Items.Contains(item))
                                {
                                    // Заполнение начального списка
                                    resultList.Add(item);
                                }

                            }
                            else
                            {
                                // Удаление после нажатия на кнопку - кнопка привязана к команде
                                resultList.Remove(item);
                            }
                        });
                }).Subscribe();

                resultList.Connect().SubscribeSafe(observer);

                return new CompositeDisposable();

            });
        }
    }
}
