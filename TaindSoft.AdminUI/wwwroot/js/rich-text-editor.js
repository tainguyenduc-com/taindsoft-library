// rich-text-editor.js - Quill.js ES module for Blazor AdminUI shared component
// Serves as _content/TaindSoft.AdminUI/js/rich-text-editor.js

window.adminQuillEditors = window.adminQuillEditors || {};

function initEditor(containerId, initialContent, dotNetRef) {
    if (window.adminQuillEditors[containerId]) return;
    if (!window.Quill) {
        console.warn('AdminRichText: Quill is not loaded');
        return;
    }

    const toolbarOptions = [
        ['bold', 'italic', 'underline', 'strike'],
        ['blockquote', 'code-block'],
        [{ 'header': 1 }, { 'header': 2 }],
        [{ 'list': 'ordered' }, { 'list': 'bullet' }],
        ['link', 'image'],
        ['clean']
    ];

    const quill = new Quill(`#${containerId}`, {
        theme: 'snow',
        placeholder: 'Enter content...',
        modules: {
            toolbar: toolbarOptions,
            clipboard: { matchVisibility: true }
        },
        formats: [
            'bold', 'italic', 'underline', 'strike',
            'blockquote', 'code-block',
            'header', 'list',
            'link', 'image'
        ]
    });

    if (initialContent && initialContent.trim() !== '') {
        quill.root.innerHTML = initialContent;
    }

    if (dotNetRef) {
        const toolbar = quill.getModule('toolbar');
        toolbar.addHandler('image', () => {
            dotNetRef.invokeMethodAsync('RequestImageInsert')
                .catch(e => console.error('RequestImageInsert failed', e));
        });

        let debounceTimer = null;
        quill.on('text-change', () => {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => {
                const html = quill.root.innerHTML;
                dotNetRef.invokeMethodAsync('OnContentChanged', html)
                    .catch(e => console.error('OnContentChanged failed', e));
            }, 300);
        });
    }

    window.adminQuillEditors[containerId] = quill;
    // also register in quillEditors map so editor-bridge.js image insert works too
    window.quillEditors = window.quillEditors || {};
    window.quillEditors[containerId] = quill;
}

function getEditorContent(containerId) {
    const quill = window.adminQuillEditors[containerId];
    return quill ? quill.root.innerHTML : '';
}

function setEditorContent(containerId, content) {
    const quill = window.adminQuillEditors[containerId];
    if (quill) quill.root.innerHTML = content || '';
}

function disposeEditor(containerId) {
    delete window.adminQuillEditors[containerId];
    if (window.quillEditors) delete window.quillEditors[containerId];
}

// Backward-compat: legacy non-module callers
window.richTextEditor = {
    initializeQuill: function (elementId, dotNetRef) {
        initEditor(elementId, '', dotNetRef);
    }
};

// Expose non-module API for direct callers
window.adminQuillApi = window.adminQuillApi || {};
window.adminQuillApi.init = initEditor;
window.adminQuillApi.getContent = getEditorContent;
window.adminQuillApi.setContent = setEditorContent;
window.adminQuillApi.dispose = disposeEditor;
