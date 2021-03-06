using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using TdLib;
using Tel.Egram.Authentication;
using Tel.Egram.Components.Authentication;
using Tel.Egram.Components.Workspace;
using Tel.Egram.Models.Application;
using Tel.Egram.Models.Application.Popup;
using Tel.Egram.Models.Application.Startup;
using Tel.Egram.Models.Authentication;
using Tel.Egram.Models.Workspace;
using Tel.Egram.Utils;
using Tel.Egram.Utils.TdLib;

namespace Tel.Egram.Components.Application
{
    public class ApplicationController : Controller<MainWindowModel>
    {
        private IController<AuthenticationModel> _authenticationController;
        private IController<WorkspaceModel> _workspaceController;

        public ApplicationController(
            ISchedulers schedulers,
            IAuthenticator authenticator,
            IApplicationPopupController applicationPopupController,
            IAgent agent)
        {
            BindConnection(schedulers, agent)
                .DisposeWith(this);

            BindAuthenticator(schedulers, authenticator)
                .DisposeWith(this);

            BindPopup(schedulers, applicationPopupController)
                .DisposeWith(this);
        }
        
        private IDisposable BindConnection(ISchedulers schedulers, IAgent agent)
        {
            return agent.Updates.OfType<TdApi.Update.UpdateConnectionState>()
                .ObserveOn(schedulers.Main)
                .Subscribe(update => 
                {
                    switch (update.State)
                    {
                        case TdApi.ConnectionState.ConnectionStateConnecting _:
                            UpdateConnectionState(ConnectionState.Connecting);
                            break;
                        case TdApi.ConnectionState.ConnectionStateConnectingToProxy _:
                            UpdateConnectionState(ConnectionState.ConnectingToProxy);
                            break;
                        case TdApi.ConnectionState.ConnectionStateReady _:
                            UpdateConnectionState(ConnectionState.Ready);
                            break;
                        case TdApi.ConnectionState.ConnectionStateUpdating _:
                            UpdateConnectionState(ConnectionState.Updating);
                            break;
                        case TdApi.ConnectionState.ConnectionStateWaitingForNetwork _:
                            UpdateConnectionState(ConnectionState.WaitingForNetwork);
                            break;
                    }
                });
        }

        private IDisposable BindAuthenticator(
            ISchedulers schedulers,
            IAuthenticator authenticator)
        {
            return authenticator
                .ObserveState()
                .ObserveOn(schedulers.Main)
                .Subscribe(state =>
                {   
                    switch (state)
                    {
                        case TdApi.AuthorizationState.AuthorizationStateWaitTdlibParameters _:
                            GoToStartupPage();
                            authenticator.SetupParameters()
                                .Subscribe()
                                .DisposeWith(this);
                            break;
                    
                        case TdApi.AuthorizationState.AuthorizationStateWaitEncryptionKey _:
                            GoToStartupPage();
                            authenticator.CheckEncryptionKey()
                                .Subscribe()
                                .DisposeWith(this);
                            break;
                    
                        case TdApi.AuthorizationState.AuthorizationStateWaitPhoneNumber _:
                        case TdApi.AuthorizationState.AuthorizationStateWaitCode _:
                        case TdApi.AuthorizationState.AuthorizationStateWaitPassword _:
                            GoToAuthenticationPage();
                            break;
                
                        case TdApi.AuthorizationState.AuthorizationStateReady _:
                            GoToWorkspacePage();
                            break;
                    }
                });
        }

        private IDisposable BindPopup(
            ISchedulers schedulers,
            IApplicationPopupController controller)
        {
            return Observable.FromEventPattern<PopupModel>(
                    h => controller.ContextChanged += h,
                    h => controller.ContextChanged -= h)
                .Select(e => e.EventArgs)
                .SubscribeOn(schedulers.Pool)
                .ObserveOn(schedulers.Main)
                .Subscribe(popupModel =>
                {
                    Model.PopupModel = popupModel ?? new PopupModel
                    {
                        IsPopupVisible = false
                    };
                });
        }

        private void GoToStartupPage()
        {
            if (Model.StartupModel == null)
            {
                Model.StartupModel = new StartupModel();
            }

            Model.WorkspaceModel = Deactivate(ref _workspaceController);
            Model.AuthenticationModel = Deactivate(ref _authenticationController);
            
            Model.PageIndex = (int) Page.Initial;
        }

        private void GoToAuthenticationPage()
        {
            if (Model.AuthenticationModel == null)
            {
                Model.AuthenticationModel = Activate(ref _authenticationController);
            }
            
            Model.PageIndex = (int) Page.Authentication;

            Model.StartupModel = null;
            Model.WorkspaceModel = Deactivate(ref _workspaceController);
        }

        private void GoToWorkspacePage()
        {
            if (Model.WorkspaceModel == null)
            {
                Model.WorkspaceModel = Activate(ref _workspaceController);
            }
            
            Model.PageIndex = (int) Page.Workspace;
            
            Model.StartupModel = null;
            Model.AuthenticationModel = Deactivate(ref _authenticationController);
        }

        private void UpdateConnectionState(ConnectionState state)
        {
            string[] stateTexts = new string[]
            {
                "Connecting...",
                "Connecting to proxy...",
                "Ready.",
                "Updating...",
                "Waiting for network..."
            };

            Model.ConnectionState = stateTexts[(int)state];
        }

        public override void Dispose()
        {
            _authenticationController?.Dispose();
            _workspaceController?.Dispose();
            
            base.Dispose();
        }
    }
}