﻿#region Using directives
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
#endregion

namespace Blazorise
{
    /// <summary>
    /// A classic modal overlay, in which you can include any content you want.
    /// </summary>
    public partial class Modal : BaseComponent
    {
        #region Members

        /// <summary>
        /// Flag that indicates if modal is visible.
        /// </summary>
        private bool visible;

        /// <summary>
        /// Holds the last received reason for modal closure.
        /// </summary>
        private CloseReason closeReason = CloseReason.None;

        /// <summary>
        /// Occurs when the <see cref="Visible"/> property value changes.
        /// </summary>
        public event EventHandler<ModalStateEventArgs> StateChanged;

        #endregion

        #region Methods

        protected override void BuildClasses( ClassBuilder builder )
        {
            builder.Append( ClassProvider.Modal() );
            builder.Append( ClassProvider.ModalFade() );
            builder.Append( ClassProvider.ModalVisible( Visible ) );

            base.BuildClasses( builder );
        }

        protected override void BuildStyles( StyleBuilder builder )
        {
            builder.Append( StyleProvider.ModalShow(), Visible );

            base.BuildStyles( builder );
        }

        /// <summary>
        /// Opens the modal dialog.
        /// </summary>
        public void Show()
        {
            if ( Visible )
                return;

            Visible = true;

            StateHasChanged();
        }

        /// <summary>
        /// Fires the modal dialog closure process.
        /// </summary>
        public void Hide()
        {
            Hide( CloseReason.UserClosing );
        }

        internal void Hide( CloseReason closeReason )
        {
            if ( !Visible )
                return;

            this.closeReason = closeReason;

            if ( IsSafeToClose() )
            {
                visible = false;

                HandleVisibilityStyles( false );
                RaiseEvents( false );

                // finally reset close reason so it doesn't interfere with internal closing by Visible property
                this.closeReason = CloseReason.None;

                StateHasChanged();
            }
        }

        private bool IsSafeToClose()
        {
            var safeToClose = true;

            var handler = Closing;

            if ( handler != null )
            {
                var args = new ModalClosingEventArgs( false, closeReason );

                foreach ( Action<ModalClosingEventArgs> subHandler in handler?.GetInvocationList() )
                {
                    subHandler( args );

                    if ( args.Cancel )
                    {
                        safeToClose = false;
                    }
                }
            }

            return safeToClose;
        }

        private void HandleVisibilityStyles( bool visible )
        {
            if ( visible )
            {
                ExecuteAfterRender( async () =>
                {
                    await JSRunner.OpenModal( ElementRef, ElementId );
                } );
            }
            else
            {
                ExecuteAfterRender( async () =>
                {
                    await JSRunner.CloseModal( ElementRef, ElementId );
                } );
            }

            DirtyClasses();
            DirtyStyles();
        }

        private void RaiseEvents( bool visible )
        {
            StateChanged?.Invoke( this, new ModalStateEventArgs( visible ) );

            if ( !visible )
            {
                Closed.InvokeAsync( null );
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Defines the visibility of modal dialog.
        /// </summary>
        [Parameter]
        public bool Visible
        {
            get => visible;
            set
            {
                // prevent modal from calling the same code multiple times
                if ( value == visible )
                    return;

                if ( value == true )
                {
                    visible = true;

                    HandleVisibilityStyles( true );
                    RaiseEvents( true );
                }
                else if ( value == false && IsSafeToClose() )
                {
                    visible = false;

                    HandleVisibilityStyles( false );
                    RaiseEvents( false );
                }
            }
        }

        /// <summary>
        /// Occurs before the modal is closed.
        /// </summary>
        [Parameter] public Action<ModalClosingEventArgs> Closing { get; set; }

        /// <summary>
        /// Occurs after the modal has closed.
        /// </summary>
        [Parameter] public EventCallback Closed { get; set; }

        [Parameter] public RenderFragment ChildContent { get; set; }

        #endregion
    }
}
