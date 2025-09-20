using System;
using System.Windows.Forms;

namespace EliteDataRelay.UI
{
    /// <summary>
    /// Interface for cargo form UI management
    /// </summary>
    public interface ICargoFormUI : IDisposable, IUIEventManager, IDataDisplayManager, IOverlayManager, IUIStateManager
    {
        /// <summary>
        /// Initialize the UI components and layout
        /// </summary>
        /// <param name="form">The main form to initialize</param>
        void InitializeUI(Form form);
    }
}