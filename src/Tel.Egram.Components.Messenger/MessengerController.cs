using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using Tel.Egram.Components.Messenger.Catalog;
using Tel.Egram.Components.Messenger.Editor;
using Tel.Egram.Components.Messenger.Explorer;
using Tel.Egram.Components.Messenger.Informer;
using Tel.Egram.Messaging.Chats;
using Tel.Egram.Models.Messenger;
using Tel.Egram.Models.Messenger.Catalog;
using Tel.Egram.Models.Messenger.Editor;
using Tel.Egram.Models.Messenger.Explorer;
using Tel.Egram.Models.Messenger.Informer;
using Tel.Egram.Utils;

namespace Tel.Egram.Components.Messenger
{
    public class MessengerController : Controller<MessengerModel>
    {
        private IController<CatalogModel> _catalogController;
        private IController<InformerModel> _informerController;
        private IController<ExplorerModel> _explorerController;
        private IController<EditorModel> _editorController;

        public MessengerController(
            Section section,
            ISchedulers schedulers)
        {
            BindCatalog(section).DisposeWith(this);
            BindInformer(schedulers).DisposeWith(this);
            BindExplorer(schedulers).DisposeWith(this);
            BindEditor(schedulers).DisposeWith(this);
        }

        private IDisposable BindCatalog(Section section)
        {
            Model.CatalogModel = Activate(section, ref _catalogController);

            return Disposable.Empty;
        }

        private IDisposable BindInformer(ISchedulers schedulers)
        {
            Model.InformerModel = InformerModel.Hidden();
            
            return SubscribeToTarget(schedulers, target =>
            {
                Model.InformerModel = Activate(target, ref _informerController);
            });
        }

        private IDisposable BindExplorer(ISchedulers schedulers)
        {
            Model.ExplorerModel = ExplorerModel.Hidden();
            
            return SubscribeToTarget(schedulers, target =>
            {
                Model.ExplorerModel = Activate(target, ref _explorerController);
            });
        }

        private IDisposable BindEditor(ISchedulers schedulers)
        {
            Model.EditorModel = EditorModel.Hidden();
            
            return SubscribeToTarget(schedulers, target =>
            {
                Model.EditorModel = Activate(target, ref _editorController);
            });
        }

        private IDisposable SubscribeToTarget(ISchedulers schedulers, Action<Target> action)
        {
            return Model.WhenAnyValue(ctx => ctx.CatalogModel.SelectedEntry)
                .SubscribeOn(schedulers.Pool)
                .ObserveOn(schedulers.Main)
                .Subscribe(entry =>
                {
                    if (entry?.Target != null)
                    {
                        action(entry.Target);
                    }
                });
        }
    }
}