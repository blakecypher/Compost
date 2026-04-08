/**
 * Compost Mind Map - Interactive mind map using Cytoscape.js
 * Handles drag-drop, zoom, pan, and node management
 * Splunk-inspired dark theme
 */

class CompostMindMap {
    constructor(containerId, options = {}) {
        this.containerId = containerId;
        this.options = {
            projectId: options.projectId || '',
            mapId: options.mapId || '',
            apiBase: options.apiBase || '/MindMap',
            editable: options.editable !== false,
            ...options
        };
        
        this.cy = null;
        this.nodeTemplates = [];
        this.initialize();
    }

    // Get icon for node type
    getNodeTypeIcon(nodeType) {
        const iconMap = {
            'Root': 'fas fa-project-diagram',
            'Idea': 'fas fa-lightbulb',
            'Requirement': 'fas fa-list-check',
            'Question': 'fas fa-question',
            'Action': 'fas fa-play',
            'Decision': 'fas fa-check',
            'Risk': 'fas fa-exclamation-triangle',
            'Note': 'fas fa-sticky-note'
        };
        return iconMap[nodeType] || 'fas fa-circle';
    }

    initialize() {
        // Check if container exists
        const container = document.getElementById(this.containerId);
        if (!container) {
            console.error(`Container with id '${this.containerId}' not found`);
            return;
        }
        
        try {
            const isDark = this.isDarkMode();
            
            // Initialize Cytoscape
            this.cy = cytoscape({
                container: container,
                
                style: this.getThemedStyle(isDark),
                
                // Cytoscape layout options
                layout: {
                    name: 'breadthfirst',
                    animate: true,
                    animationDuration: 500,
                    fit: true,
                    padding: 50,
                    directed: true,
                    spacingFactor: 1.2,
                    roots: undefined,
                    maximal: false
                },
                
                // Interaction options
                minZoom: 0.1,
                maxZoom: 3,
                zoomingEnabled: true,
                userPanningEnabled: true,
                boxSelectionEnabled: true,
                
                // Event handlers
                ready: () => this.onReady(),
            });
            
            this.initThemeObserver();
            console.log('Cytoscape initialized successfully');
            
        } catch (error) {
            console.error('Error initializing Cytoscape:', error);
            this.cy = null;
        }
    }

    isDarkMode() {
        const theme = document.documentElement.getAttribute('data-bs-theme') || document.documentElement.getAttribute('data-theme') || 'auto';
        if (theme === 'dark') return true;
        if (theme === 'light') return false;
        // Auto: check system preference
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    }

    initThemeObserver() {
        // Observe html[data-bs-theme] or [data-theme] attribute changes
        const observer = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                if (mutation.attributeName === 'data-bs-theme' || mutation.attributeName === 'data-theme') {
                    this.refreshTheme();
                }
            });
        });

        observer.observe(document.documentElement, {
            attributes: true,
            attributeFilter: ['data-bs-theme', 'data-theme']
        });

        // Also listen for system theme changes if in auto mode
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
            const theme = document.documentElement.getAttribute('data-bs-theme') || document.documentElement.getAttribute('data-theme');
            if (theme === 'auto' || !theme) {
                this.refreshTheme();
            }
        });
    }

    refreshTheme() {
        if (!this.cy) return;
        const isDark = this.isDarkMode();
        console.log('Refreshing mind map theme. IsDark:', isDark);
        this.cy.style(this.getThemedStyle(isDark));
    }

    getThemedStyle(isDark) {
        const primaryColor = isDark ? '#ff6b35' : '#2563eb';
        const nodeBg = isDark ? '#2d2d2d' : '#f5f7fa';
        const nodeText = isDark ? '#ffffff' : '#1e293b';
        const edgeColor = isDark ? '#444444' : '#b8c4d4';
        const shadowColor = isDark ? 'rgba(255, 107, 53, 0.4)' : 'rgba(37, 99, 235, 0.2)';
        const outlineColor = isDark ? 'rgba(0,0,0,0.8)' : 'rgba(245,247,250,0.9)';

        return [
            // Base node style
            {
                selector: 'node',
                style: {
                    'background-color': 'data(color)',
                    'label': 'data(label)',
                    'width': '80px',
                    'height': '80px',
                    'text-valign': 'center',
                    'text-halign': 'center',
                    'font-size': '12px',
                    'font-weight': '600',
                    'font-family': 'Inter, -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif',
                    'color': nodeText,
                    'text-outline-color': outlineColor,
                    'text-outline-width': 2,
                    'text-wrap': 'wrap',
                    'text-max-width': '100px',
                    'border-width': 2,
                    'border-color': primaryColor,
                    'border-opacity': 0.8,
                    'shadow-blur': isDark ? 20 : 10,
                    'shadow-color': shadowColor,
                    'shadow-opacity': 1,
                    'shadow-offset-x': 0,
                    'shadow-offset-y': 0,
                    'transition-property': 'background-color, border-color, border-width, width, height, shadow-blur, transform',
                    'transition-duration': '0.4s, 0.4s, 0.4s, 0.3s, 0.3s, 0.4s',
                    'transition-timing-function': 'ease-out',
                    'overlay-opacity': 0,
                    'overlay-padding': 0,
                    'z-index': 'data(level)',
                    'shape': 'roundrectangle',
                    'background-blacken': isDark ? 0.1 : 0,
                    'background-image': 'data(icon)',
                    'background-fit': 'contain',
                    'background-position-x': '50%',
                    'background-position-y': '20%',
                    'background-width-percentage': '40%',
                    'background-height-percentage': '40%'
                }
            },
            // Root node special styling
            {
                selector: 'node[type = "Root"]',
                style: {
                    'width': '100px',
                    'height': '100px',
                    'font-size': '14px',
                    'font-weight': '700',
                    'border-width': 3,
                    'border-color': primaryColor,
                    'shadow-blur': isDark ? 30 : 15,
                    'shadow-color': isDark ? 'rgba(255, 107, 53, 0.6)' : 'rgba(37, 99, 235, 0.3)',
                    'shape': 'hexagon',
                    'background-gradient-stop-colors': isDark ? '#2c5f2d #1a3d1a' : '#4caf50 #2e7d32',
                    'background-gradient-direction': 'to-bottom'
                }
            },
            // Hover effects
            {
                selector: 'node:hover',
                style: {
                    'border-width': 3,
                    'border-color': primaryColor,
                    'shadow-blur': 25,
                    'shadow-color': primaryColor,
                    'transform': 'scale(1.1)',
                    'z-index': 1000,
                    'transition-duration': '0.2s'
                }
            },
            // Selected node
            {
                selector: 'node:selected',
                style: {
                    'border-width': 4,
                    'border-color': primaryColor,
                    'shadow-blur': 30,
                    'shadow-color': primaryColor,
                    'transform': 'scale(1.15)',
                    'z-index': 1001
                }
            },
            // Edge styling
            {
                selector: 'edge',
                style: {
                    'width': 3,
                    'line-color': edgeColor,
                    'target-arrow-color': edgeColor,
                    'target-arrow-shape': 'triangle',
                    'target-arrow-size': 8,
                    'curve-style': 'bezier',
                    'opacity': 0.8,
                    'shadow-blur': isDark ? 5 : 2,
                    'shadow-color': 'rgba(0,0,0,0.2)',
                    'transition-property': 'line-color, width, opacity',
                    'transition-duration': '0.3s'
                }
            },
            // Hover edge
            {
                selector: 'edge:hover',
                style: {
                    'width': 4,
                    'line-color': primaryColor,
                    'target-arrow-color': primaryColor,
                    'opacity': 1,
                    'shadow-blur': 10,
                    'shadow-color': primaryColor
                }
            },
            // Selected edge
            {
                selector: 'edge:selected',
                style: {
                    'width': 5,
                    'line-color': primaryColor,
                    'target-arrow-color': primaryColor,
                    'opacity': 1
                }
            }
        ];
    }

    onReady() {
        // Add a small delay to ensure Cytoscape is fully initialized
        setTimeout(() => {
            if (!this.cy) {
                console.error('Cytoscape instance not initialized in onReady');
                return;
            }
            
            this.loadNodesForMap(this.options.mapId);
            
            // Add selection event handlers
            this.cy.on('select', 'node', (event) => {
                const node = event.target;
                console.log('Node selected:', node.id());
                this.showNodeDetails(node);
            });
            
            this.cy.on('unselect', 'node', () => {
                console.log('Node unselected');
                this.hideNodeDetails();
            });
            
            this.cy.on('select', 'edge', (event) => {
                const edge = event.target;
                console.log('Edge selected:', edge.id());
                this.showEdgeDetails(edge);
            });
            
            this.cy.on('unselect', 'edge', () => {
                console.log('Edge unselected');
                this.hideNodeDetails();
            });
            
            // Add background tap to clear selection
            this.cy.on('tap', (event) => {
                if (event.target === this.cy) {
                    console.log('Background tapped, clearing selection');
                    this.cy.elements().unselect();
                }
            });
            console.log('Mind map selection events initialized');
        }, 100);
    }

    async loadNodesForMap(mapId) {
        if (!mapId) return;
        
        try {
            const response = await fetch(`${this.options.apiBase}/GetNodes/${mapId}`);
            const data = await response.json();
            
            if (data.success && data.nodes) {
                this.addNodes(data.nodes);
            }
        } catch (error) {
            console.error('Error loading nodes:', error);
        }
    }

    async loadNodesForContext(projectId) {
        if (!projectId) return;
        
        try {
            const response = await fetch(`${this.options.apiBase}/GetNodesForContext/${projectId}`);
            const data = await response.json();
            
            if (data.success && data.nodes) {
                this.addNodes(data.nodes);
            }
        } catch (error) {
            console.error('Error loading nodes:', error);
        }
    }

    addNodes(nodes) {
        // Ensure Cytoscape instance is initialized
        if (!this.cy) {
            console.error('Cytoscape instance not initialized');
            return;
        }
        
        if (!nodes || nodes.length === 0) {
            console.log('No nodes to add');
            return;
        }
        
        const elements = [];
        
        // Add nodes
        nodes.forEach(node => {
            const icon = this.getNodeTypeIcon(node.nodeType);
            elements.push({
                data: {
                    id: node.id,
                    label: node.text,
                    type: node.nodeType,
                    color: node.color,
                    icon: icon, // Set icon based on node type
                    level: node.level,
                    sourceText: node.sourceText,
                    sourceType: node.sourceType,
                    notes: node.notes,
                    status: node.status,
                    isPromoted: node.isPromoted,
                    promotedToId: node.promotedToId,
                    createdAt: node.createdAt
                }
            });
        });
        
        // Add edges
        nodes.forEach(node => {
            if (node.parentId) {
                elements.push({
                    data: {
                        id: `${node.parentId}-${node.id}`,
                        source: node.parentId,
                        target: node.id
                    }
                });
            }
        });
        
        try {
            this.cy.add(elements);
            // Apply hierarchical layout after adding nodes
            this.cy.layout({
                name: 'breadthfirst',
                animate: true,
                animationDuration: 500,
                fit: true,
                padding: 50,
                directed: true,
                spacingFactor: 1.2
            }).run();
            this.cy.fit(undefined, 50);
        } catch (error) {
            console.error('Error adding elements to Cytoscape:', error);
        }
    }

    // Fit to view
    fitToView() {
        if (!this.cy) {
            console.error('Cytoscape instance not initialized');
            return;
        }
        this.cy.fit(null, 50);
    }

    // Show node details in the panel
    showNodeDetails(node) {
        console.log('Showing node details for:', node.id());
        const panel = document.getElementById('node-details-panel');
        if (!panel) {
            console.error('node-details-panel not found');
            return;
        }
        
        const placeholder = document.getElementById('select-node-placeholder');
        const content = document.getElementById('node-details-content');
        if (!content) {
            console.error('node-details-content not found');
            return;
        }

        if (placeholder) placeholder.style.display = 'none';
        
        const data = node.data();
        
        content.innerHTML = `
            <div class="mb-4">
                <h5 class="text-theme-mindmaps mb-3">
                    <i class="fas ${data.icon || 'fas fa-circle'}"></i> ${data.label}
                </h5>
                
                <table class="table table-sm mb-0">
                    <tr>
                        <td class="text-muted" style="width: 40%;"><strong>Type:</strong></td>
                        <td><span class="badge border border-theme-mindmaps text-theme-mindmaps">${data.type || 'Unknown'}</span></td>
                    </tr>
                    <tr>
                        <td class="text-muted"><strong>Level:</strong></td>
                        <td><span class="badge bg-secondary">${data.level || 1}</span></td>
                    </tr>
                    <tr>
                        <td class="text-muted"><strong>Status:</strong></td>
                        <td><span class="badge bg-success">${data.status || 'Active'}</span></td>
                    </tr>
                    <tr>
                        <td class="text-muted"><strong>Source:</strong></td>
                        <td><span class="badge bg-info text-dark">${data.sourceType || 'Manual'}</span></td>
                    </tr>
                </table>
            </div>

            <div class="mb-4">
                <h6 class="text-muted mb-2 small fw-bold">SOURCE TEXT</h6>
                <div class="card bg-light border-0" style="max-height: 200px; overflow-y: auto;">
                    <div class="card-body p-2">
                        <small class="text-muted" style="line-height: 1.4; display: block;">
                            ${data.sourceText || data.label || 'No source text available'}
                        </small>
                    </div>
                </div>
            </div>

            ${data.notes ? `
                <div class="mb-4">
                    <h6 class="text-muted mb-2 small fw-bold">NOTES</h6>
                    <div class="card bg-light border-0" style="max-height: 150px; overflow-y: auto;">
                        <div class="card-body p-2">
                            <small style="line-height: 1.4; display: block;">${data.notes}</small>
                        </div>
                    </div>
                </div>
            ` : ''}

            <div class="mt-auto pt-3 border-top border-secondary">
                <div class="d-grid gap-2">
                    <button class="btn btn-sm btn-outline-theme-mindmaps" onclick="window.compostMindMap.editNode('${data.id}')">
                        <i class="fas fa-edit"></i> Edit Node
                    </button>
                    <button class="btn btn-sm btn-outline-danger" onclick="window.compostMindMap.deleteNode('${data.id}')">
                        <i class="fas fa-trash"></i> Delete
                    </button>
                    ${data.type !== 'Root' ? `
                        <button class="btn btn-sm btn-theme-mindmaps" onclick="window.compostMindMap.promoteNode('${data.id}')">
                            <i class="fas fa-arrow-up"></i> Promote
                        </button>
                    ` : ''}
                </div>
            </div>
        `;
        
        panel.style.display = 'block';
    }

    // Show edge details
    showEdgeDetails(edge) {
        console.log('Showing edge details for:', edge.id());
        const panel = document.getElementById('node-details-panel');
        if (!panel) {
            console.error('node-details-panel not found');
            return;
        }
        
        const placeholder = document.getElementById('select-node-placeholder');
        const content = document.getElementById('node-details-content');
        if (!content) {
            console.error('node-details-content not found');
            return;
        }

        if (placeholder) placeholder.style.display = 'none';
        
        const source = edge.source();
        const target = edge.target();
        
        content.innerHTML = `
            <div class="mb-4">
                <h5 class="text-theme-mindmaps mb-3">
                    <i class="fas fa-link"></i> Connection Details
                </h5>
                <table class="table table-sm mb-0">
                    <tr>
                        <td class="text-muted" style="width: 40%;"><strong>From:</strong></td>
                        <td>${source.data('label')}</td>
                    </tr>
                    <tr>
                        <td class="text-muted"><strong>To:</strong></td>
                        <td>${target.data('label')}</td>
                    </tr>
                    <tr>
                        <td class="text-muted"><strong>Type:</strong></td>
                        <td><span class="badge bg-secondary">Relationship</span></td>
                    </tr>
                </table>
            </div>

            <div class="mt-auto pt-3 border-top border-secondary">
                <div class="d-grid gap-2">
                    <button class="btn btn-sm btn-outline-danger" onclick="window.compostMindMap.deleteEdge('${edge.id()}')">
                        <i class="fas fa-unlink"></i> Remove
                    </button>
                </div>
            </div>
        `;
        
        panel.style.display = 'block';
    }

    // Hide node details panel
    hideNodeDetails() {
        // If something is still selected (e.g., clicked from one node straight to another),
        // don't hide the details as the 'select' event will handle the update.
        if (this.cy && this.cy.elements(':selected').length > 0) {
            return;
        }

        const placeholder = document.getElementById('select-node-placeholder');
        const content = document.getElementById('node-details-content');
        
        if (placeholder) placeholder.style.display = 'block';
        if (content) content.innerHTML = '';
        
        // We keep the panel visible now because it's a sidebar
    }

    // Placeholder methods for node actions
    editNode(nodeId) {
        const node = this.cy.getElementById(nodeId);
        if (node.length === 0) {
            console.error('Node not found:', nodeId);
            return;
        }

        const nodeData = node.data();
        
        // Create modal dialog for node editing
        const modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.innerHTML = `
            <div class="modal-dialog">
                <div class="modal-content card border-theme-mindmaps">
                    <div class="modal-header bg-theme-mindmaps text-on-theme-mindmaps border-0">
                        <h5 class="modal-title">Edit Node: ${nodeData.label}</h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <div class="mb-3">
                            <label class="form-label text-muted small fw-bold">NODE TEXT</label>
                            <input type="text" id="node-text-input" class="form-control border-theme-mindmaps" value="${nodeData.label || ''}" placeholder="Enter node text">
                        </div>
                        <div class="mb-3">
                            <label class="form-label text-muted small fw-bold">NODE TYPE</label>
                            <select id="node-type-select" class="form-select border-theme-mindmaps">
                                <option value="Root" ${nodeData.type === 'Root' ? 'selected' : ''}>Root</option>
                                <option value="Idea" ${nodeData.type === 'Idea' ? 'selected' : ''}>Idea</option>
                                <option value="Requirement" ${nodeData.type === 'Requirement' ? 'selected' : ''}>Requirement</option>
                                <option value="Question" ${nodeData.type === 'Question' ? 'selected' : ''}>Question</option>
                                <option value="Action" ${nodeData.type === 'Action' ? 'selected' : ''}>Action</option>
                                <option value="Decision" ${nodeData.type === 'Decision' ? 'selected' : ''}>Decision</option>
                                <option value="Risk" ${nodeData.type === 'Risk' ? 'selected' : ''}>Risk</option>
                                <option value="Note" ${nodeData.type === 'Note' ? 'selected' : ''}>Note</option>
                            </select>
                        </div>
                        <div class="mb-3">
                            <label class="form-label text-muted small fw-bold">COLOR</label>
                            <input type="color" id="node-color-input" class="form-control form-control-color border-theme-mindmaps" value="${nodeData.color || '#4CAF50'}">
                        </div>
                        <div class="mb-3">
                            <label class="form-label text-muted small fw-bold">NOTES</label>
                            <textarea id="node-notes-input" class="form-control border-theme-mindmaps" rows="3" placeholder="Add notes...">${nodeData.notes || ''}</textarea>
                        </div>
                        <div class="mb-3">
                            <label class="form-label text-muted small fw-bold">SOURCE TEXT</label>
                            <textarea id="node-source-text-input" class="form-control border-theme-mindmaps" rows="4" placeholder="Source text...">${nodeData.sourceText || ''}</textarea>
                        </div>
                    </div>
                    <div class="modal-footer border-0">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn btn-theme-mindmaps" id="save-node-edit">Save Changes</button>
                    </div>
                </div>
            </div>
        `;

        document.body.appendChild(modal);
        
        // Initialize Bootstrap modal
        const bsModal = new bootstrap.Modal(modal);
        bsModal.show();

        // Handle save
        modal.querySelector('#save-node-edit').addEventListener('click', async () => {
            const updatedData = {
                title: modal.querySelector('#node-text-input').value,
                nodeType: modal.querySelector('#node-type-select').value,
                color: modal.querySelector('#node-color-input').value,
                content: modal.querySelector('#node-notes-input').value,
                sourceText: modal.querySelector('#node-source-text-input').value
            };

            if (!updatedData.title.trim()) {
                this.showNotification('Node text is required', 'error');
                return;
            }

            try {
                // Update node via API
                const response = await fetch(`${this.options.apiBase}/ApiUpdateNode?mapId=${encodeURIComponent(this.options.mapId)}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        id: nodeId,
                        ...updatedData
                    })
                });

                if (response.ok) {
                    const updatedNode = await response.json();
                    
                    // Update node in Cytoscape
                    node.data('label', updatedData.title);
                    node.data('type', updatedData.nodeType);
                    node.data('color', updatedData.color);
                    node.data('notes', updatedData.content);
                    node.data('sourceText', updatedData.sourceText);
                    node.data('icon', this.getNodeTypeIcon(updatedData.nodeType));
                    
                    // Update node style
                    node.style('background-color', updatedData.color);
                    
                    // Refresh node details panel if this node is selected
                    if (node.selected()) {
                        this.showNodeDetails(node);
                    }
                    
                    this.showNotification('Node updated successfully', 'success');
                    bsModal.hide();
                } else {
                    throw new Error('Failed to update node');
                }
            } catch (error) {
                console.error('Error updating node:', error);
                this.showNotification('Failed to update node', 'error');
            }
        });

        // Cleanup on hide
        modal.addEventListener('hidden.bs.modal', () => {
            document.body.removeChild(modal);
        });
    }

    deleteNode(nodeId) {
        const node = this.cy.getElementById(nodeId);
        if (node.length === 0) {
            console.error('Node not found:', nodeId);
            return;
        }

        const nodeData = node.data();
        
        // Confirm deletion
        if (!confirm(`Are you sure you want to delete "${nodeData.label}"? This action cannot be undone.`)) {
            return;
        }

        // Don't allow deletion of root nodes
        if (nodeData.type === 'Root') {
            this.showNotification('Root nodes cannot be deleted', 'error');
            return;
        }

        this.deleteNodeAsync(nodeId);
    }

    async deleteNodeAsync(nodeId) {
        try {
            // Delete node via API
            const response = await fetch(`${this.options.apiBase}/ApiDeleteNode?mapId=${encodeURIComponent(this.options.mapId)}&nodeId=${encodeURIComponent(nodeId)}`, {
                method: 'DELETE'
            });

            if (response.ok) {
                // Remove node from Cytoscape
                const node = this.cy.getElementById(nodeId);
                node.remove();
                
                // Hide node details panel
                this.hideNodeDetails();
                
                this.showNotification('Node deleted successfully', 'success');
            } else {
                throw new Error('Failed to delete node');
            }
        } catch (error) {
            console.error('Error deleting node:', error);
            this.showNotification('Failed to delete node', 'error');
        }
    }

    deleteEdge(edgeId) {
        const edge = this.cy.getElementById(edgeId);
        if (edge.length === 0) {
            console.error('Edge not found:', edgeId);
            return;
        }

        const sourceNode = edge.source();
        const targetNode = edge.target();
        
        // Confirm deletion
        if (!confirm(`Are you sure you want to remove the connection from "${sourceNode.data('label')}" to "${targetNode.data('label')}"?`)) {
            return;
        }

        // Remove edge from Cytoscape
        edge.remove();
        
        // Hide node details panel
        this.hideNodeDetails();
        
        this.showNotification('Connection removed successfully', 'success');
    }

    async promoteNode(nodeId) {
        const node = this.cy.getElementById(nodeId);
        if (!node) {
            console.error('Node not found:', nodeId);
            return;
        }

        // Check if already promoted
        if (node.data('isPromoted')) {
            this.showNotification('Node is already promoted', 'warning');
            return;
        }

        // Show promotion dialog with AI agent
        this.showPromotionDialog(node);
    }

    showPromotionDialog(node) {
        // Create modal overlay
        const modalOverlay = document.createElement('div');
        modalOverlay.className = 'promote-modal-overlay';
        modalOverlay.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            z-index: 10000;
            display: flex;
            align-items: center;
            justify-content: center;
        `;

        // Create modal content with AI agent on the right
        const modalContent = document.createElement('div');
        modalContent.className = 'promote-modal-content';
        modalContent.style.cssText = `
            border-radius: 8px;
            width: 90%;
            max-width: 1200px;
            height: 80vh;
            display: flex;
            overflow: hidden;
        `;

        modalContent.innerHTML = `
            <div class="promote-modal-left" style="flex: 1; padding: 2rem; overflow-y: auto;">
                <h3 class="promote-modal-title" style="margin-bottom: 1.5rem;">
                    <i class="fas fa-arrow-up"></i> Promote Node
                </h3>
                <div class="promote-modal-node-info" style="padding: 1.25rem; border-radius: 8px; margin-bottom: 1.5rem;">
                    <h4 class="promote-modal-node-title" style="margin-bottom: 0.75rem; font-weight: 600;">${node.data('label')}</h4>
                    <p class="promote-modal-node-text" style="font-size: 0.95rem; line-height: 1.5; margin-bottom: 0;">${node.data('sourceText') || 'No source text'}</p>
                </div>
                <div style="margin-bottom: 1rem;">
                    <label class="promote-modal-label" style="display: block; margin-bottom: 0.5rem;">Promotion Type:</label>
                    <select id="promotion-type" class="promote-modal-select" style="width: 100%; padding: 0.5rem; border-radius: 4px;">
                        <option value="tree">Tree Node (Kanban)</option>
                        <option value="structure">Structure (if has children)</option>
                    </select>
                </div>
                <div style="margin-bottom: 1rem;">
                    <label class="promote-modal-label" style="display: block; margin-bottom: 0.5rem;">Notes (optional):</label>
                    <textarea id="promotion-notes" class="promote-modal-textarea" style="width: 100%; height: 100px; padding: 0.5rem; border-radius: 4px; resize: vertical;" placeholder="Add any notes for the promotion..."></textarea>
                </div>
                <div style="display: flex; gap: 1rem;">
                    <button id="confirm-promote" class="btn btn-primary" style="flex: 1; padding: 0.75rem; font-weight: 600;">
                        <i class="fas fa-check"></i> Promote
                    </button>
                    <button id="cancel-promote" class="btn btn-secondary" style="flex: 1; padding: 0.75rem;">
                        <i class="fas fa-times"></i> Cancel
                    </button>
                </div>
            </div>
            <div class="promote-modal-right" style="width: 400px; padding: 2rem;">
                <h3 class="promote-modal-title" style="margin-bottom: 1rem;">
                    <i class="fas fa-robot"></i> AI Assistant
                </h3>
                <div id="ai-chat" class="promote-modal-ai-chat" style="height: calc(100% - 3rem); overflow-y: auto;">
                    <div class="promote-modal-ai-message" style="padding: 1rem; border-radius: 4px; margin-bottom: 1rem;">
                        <p style="margin: 0;">I'm analyzing this node for promotion...</p>
                    </div>
                </div>
            </div>
        `;

        modalOverlay.appendChild(modalContent);
        document.body.appendChild(modalOverlay);

        // Add event listeners
        document.getElementById('confirm-promote').addEventListener('click', () => {
            this.executePromotion(node, modalOverlay);
        });

        document.getElementById('cancel-promote').addEventListener('click', () => {
            modalOverlay.remove();
        });

        // Simulate AI analysis
        this.simulateAIAnalysis(node);
    }

    simulateAIAnalysis(node) {
        const aiChat = document.getElementById('ai-chat');
        const messages = [
            "Analyzing node structure and content...",
            "Checking promotion requirements...",
            "This node appears to be a good candidate for promotion.",
            "The content is well-defined and actionable.",
            "Ready to proceed with promotion when you confirm."
        ];

        messages.forEach((message, index) => {
            setTimeout(() => {
                const messageDiv = document.createElement('div');
                messageDiv.className = 'promote-modal-ai-message';
                messageDiv.style.cssText = 'padding: 1rem; border-radius: 4px; margin-bottom: 1rem;';
                messageDiv.innerHTML = `<p style="margin: 0;">${message}</p>`;
                aiChat.appendChild(messageDiv);
                aiChat.scrollTop = aiChat.scrollHeight;
            }, (index + 1) * 800);
        });
    }

    async executePromotion(node, modalOverlay) {
        const promotionType = document.getElementById('promotion-type').value;
        const notes = document.getElementById('promotion-notes').value;

        try {
            let url;
            if (promotionType === 'structure') {
                url = `${this.options.apiBase}/ApiPromoteToStructure?mapId=${encodeURIComponent(this.options.mapId)}&nodeId=${encodeURIComponent(node.id())}`;
            } else {
                url = `${this.options.apiBase}/ApiPromoteToKanban?mapId=${encodeURIComponent(this.options.mapId)}&nodeId=${encodeURIComponent(node.id())}`;
            }

            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ notes })
            });

            if (response.ok) {
                const data = await response.json();
                modalOverlay.remove();
                this.showNotification('Node promoted successfully!', 'success');
                
                // Update node data
                node.data('isPromoted', true);
                node.data('promotedToId', data.kanbanCardId || data.treeNodeId || data.structureId);
                
                // Show notification with details
                if (data.message) {
                    this.showNotification(data.message, 'success');
                }
                
                // Optionally navigate to the refinement page
                if (data.url) {
                    setTimeout(() => {
                        window.open(data.url, '_blank');
                    }, 1000);
                }
            } else {
                const errorText = await response.text();
                throw new Error(`Promotion failed: ${response.status} ${response.statusText} - ${errorText}`);
            }
        } catch (error) {
            console.error('Error promoting node:', error);
            this.showNotification(`Failed to promote node: ${error.message}`, 'error');
        }
    }

    showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `alert alert-${type === 'error' ? 'danger' : type} animate-fade-in`;
        
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 1rem 1.5rem;
            border-radius: 4px;
            z-index: 10001;
        `;
        notification.textContent = message;
        
        document.body.appendChild(notification);
        
        setTimeout(() => {
            notification.remove();
        }, 3000);
    }

    deleteEdge(edgeId) {
        console.log('Delete edge:', edgeId);
        // TODO: Implement edge delete functionality
    }

    // Spread nodes to prevent overlapping
    spreadNodes() {
        if (!this.cy) {
            console.error('Cytoscape instance not initialized');
            return;
        }
        
        const nodes = this.cy.nodes();
        const nodeCount = nodes.length;
        
        if (nodeCount === 0) return;
        
        // Get canvas dimensions
        const container = document.getElementById(this.containerId);
        const width = container.clientWidth;
        const height = container.clientHeight;
        
        // Calculate center and radius
        const centerX = width / 2;
        const centerY = height / 2;
        const maxRadius = Math.min(width, height) * 0.4; // Use 40% of smaller dimension
        
        // Group nodes by level for better organization
        const nodesByLevel = {};
        nodes.forEach(node => {
            const level = node.data('level') || 1;
            if (!nodesByLevel[level]) {
                nodesByLevel[level] = [];
            }
            nodesByLevel[level].push(node);
        });
        
        // Get unique levels and sort them
        const levels = Object.keys(nodesByLevel).map(Number).sort((a, b) => a - b);
        
        // Position nodes by level in concentric circles
        levels.forEach((level, levelIndex) => {
            const levelNodes = nodesByLevel[level];
            const levelRadius = maxRadius * (levelIndex + 1) / levels.length;
            const angleStep = (2 * Math.PI) / levelNodes.length;
            
            levelNodes.forEach((node, index) => {
                const angle = index * angleStep;
                const x = centerX + levelRadius * Math.cos(angle);
                const y = centerY + levelRadius * Math.sin(angle);
                
                // Animate the position change
                node.animate({
                    position: { x: x, y: y }
                }, {
                    duration: 500,
                    easing: 'ease-out'
                });
            });
        });
        
        // Fit to view after spreading
        setTimeout(() => {
            this.fitToView();
        }, 600);
    }

    exportAsImage() {
        if (!this.cy) {
            console.error('Cytoscape instance not initialized');
            return;
        }
        
        const isDark = this.isDarkMode();
        const png = this.cy.png({
            output: 'blob',
            bg: isDark ? '#0f172a' : '#f5f7fa',
            full: true,
            scale: 2
        });
        
        const link = document.createElement('a');
        link.href = png;
        link.download = 'mindmap.png';
        link.click();
    }

    async applyLayout(layoutType) {
        if (!this.cy || !this.options.mapId) {
            console.error('Cytoscape instance or map ID not available');
            return;
        }

        console.log(`Applying ${layoutType} layout...`);
        console.log('Current nodes:', this.cy.nodes().length);

        try {
            const response = await fetch(`${this.options.apiBase}/ApiApplyLayout`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    mapId: this.options.mapId,
                    layoutType: layoutType
                })
            });

            if (response.ok) {
                const result = await response.json();
                console.log(`Applied ${layoutType} layout:`, result);
                console.log('Positions returned:', result.positions);
                console.log('Number of positions:', Object.keys(result.positions).length);
                
                if (result.positions) {
                    console.log('Starting batch update...');
                    this.cy.startBatch();
                    Object.entries(result.positions).forEach(([nodeId, position]) => {
                        const node = this.cy.getElementById(nodeId);
                        if (node.length > 0) {
                            // Backend returns NodePositionDto { X, Y } which might be lowercase in JSON
                            let xVal = position.x ?? position.X;
                            let yVal = position.y ?? position.Y;
                            
                            // Ensure numeric values
                            const x = Number(xVal);
                            const y = Number(yVal);
                            
                            if (!isNaN(x) && !isNaN(y)) {
                                console.log(`Updating node ${nodeId} to position:`, { x, y });
                                node.position({ x: x, y: y });
                            } else {
                                console.error(`Node ${nodeId} has invalid numeric position data:`, { xVal, yVal, original: position });
                            }
                        } else {
                            console.log(`Node ${nodeId} not found in cytoscape`);
                        }
                    });
                    this.cy.endBatch();
                    this.fitToView();
                    console.log('Batch update completed');
                    
                    // Check if nodes are still visible after layout
                    setTimeout(() => {
                        const visibleNodes = this.cy.nodes().filter(node => node.visible());
                        console.log('Visible nodes after layout:', visibleNodes.length);
                        console.log('Total nodes:', this.cy.nodes().length);
                        
                        if (visibleNodes.length === 0) {
                            console.warn('No nodes visible after layout application!');
                            this.showNotification('Layout applied but nodes may not be visible. Try zooming out or refreshing.', 'warning');
                        }
                    }, 1000);
                }

                // Show success message
                this.showNotification(`Applied ${layoutType} layout`, 'success');
            } else {
                throw new Error('Failed to apply layout');
            }
        } catch (error) {
            console.error('Error applying layout:', error);
            this.showNotification('Failed to apply layout', 'error');
        }
    }

    async updateNodeStyle(nodeId, styleUpdates) {
        if (!this.cy || !this.options.mapId) {
            console.error('Cytoscape instance or map ID not available');
            return;
        }

        try {
            const response = await fetch(`${this.options.apiBase}/ApiUpdateNodeStyle`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    mapId: this.options.mapId,
                    nodeId: nodeId,
                    ...styleUpdates
                })
            });

            if (response.ok) {
                const result = await response.json();
                console.log('Node style updated:', result);
                
                // Update node visualization
                const node = this.cy.getElementById(nodeId);
                if (node.length > 0) {
                    const style = {};
                    if (styleUpdates.color) style['background-color'] = styleUpdates.color;
                    if (styleUpdates.shape) style.shape = styleUpdates.shape;
                    if (styleUpdates.fontSize) style['font-size'] = styleUpdates.fontSize + 'px';
                    if (styleUpdates.size) {
                        style.width = styleUpdates.size + 'px';
                        style.height = styleUpdates.size + 'px';
                    }
                    if (styleUpdates.icon) {
                        style['background-image'] = styleUpdates.icon;
                        node.data('icon', styleUpdates.icon);
                    }
                    
                    node.style(style);
                }

                this.showNotification('Node style updated', 'success');
            } else {
                throw new Error('Failed to update node style');
            }
        } catch (error) {
            console.error('Error updating node style:', error);
            this.showNotification('Failed to update node style', 'error');
        }
    }

    async loadNodeTemplates() {
        try {
            const response = await fetch(`${this.options.apiBase}/ApiGetTemplates`);
            if (response.ok) {
                this.nodeTemplates = await response.json();
                console.log('Node templates loaded:', this.nodeTemplates);
            }
        } catch (error) {
            console.error('Error loading node templates:', error);
        }
    }

    createNodeFromTemplate(templateId, position) {
        const template = this.nodeTemplates?.find(t => t.id === templateId);
        if (!template) {
            console.error('Template not found:', templateId);
            return;
        }

        const icon = this.getNodeTypeIcon(templateId);
        const nodeData = {
            label: template.name,
            type: templateId,
            color: template.color,
            icon: icon, // Set icon based on template type
            shape: template.shape,
            position: position || { x: Math.random() * 600 + 100, y: Math.random() * 400 + 100 }
        };

        this.addNode(nodeData);
    }

    showNodeStyleDialog(nodeId) {
        const node = this.cy.getElementById(nodeId);
        if (node.length === 0) return;

        const nodeData = node.data();
        
        // Create modal dialog for node styling
        const modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.innerHTML = `
            <div class="modal-dialog">
                <div class="modal-content card border-theme-mindmaps">
                    <div class="modal-header bg-theme-mindmaps text-on-theme-mindmaps border-0">
                        <h5 class="modal-title">Node Style: ${nodeData.label}</h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <div class="mb-3">
                            <label class="form-label text-muted small fw-bold">NODE TYPE</label>
                            <select id="node-type-select" class="form-select border-theme-mindmaps">
                                <option value="">Select type...</option>
                                ${this.nodeTemplates?.map(t => `<option value="${t.id}" ${nodeData.nodeType === t.id ? 'selected' : ''}>${t.icon} ${t.name}</option>`).join('') || ''}
                            </select>
                        </div>
                        <div class="mb-3">
                            <label class="form-label text-muted small fw-bold">COLOR</label>
                            <input type="color" id="node-color-input" class="form-control form-control-color border-theme-mindmaps" value="${nodeData.color || '#4CAF50'}">
                        </div>
                        <div class="mb-3">
                            <label class="form-label text-muted small fw-bold">SHAPE</label>
                            <select id="node-shape-select" class="form-select border-theme-mindmaps">
                                <option value="ellipse" ${nodeData.shape === 'ellipse' ? 'selected' : ''}>Ellipse</option>
                                <option value="rectangle" ${nodeData.shape === 'rectangle' ? 'selected' : ''}>Rectangle</option>
                                <option value="roundrectangle" ${nodeData.shape === 'roundrectangle' ? 'selected' : ''}>Round Rectangle</option>
                                <option value="hexagon" ${nodeData.shape === 'hexagon' ? 'selected' : ''}>Hexagon</option>
                                <option value="diamond" ${nodeData.shape === 'diamond' ? 'selected' : ''}>Diamond</option>
                                <option value="triangle" ${nodeData.shape === 'triangle' ? 'selected' : ''}>Triangle</option>
                            </select>
                        </div>
                        <div class="mb-3">
                            <label class="form-label text-muted small fw-bold">SIZE</label>
                            <input type="range" id="node-size-input" class="form-range" min="40" max="120" value="${nodeData.width || 80}">
                            <div class="d-flex justify-content-between mt-1">
                                <small class="text-muted">40px</small>
                                <small class="text-theme-mindmaps fw-bold"><span id="size-value">${nodeData.width || 80}</span>px</small>
                                <small class="text-muted">120px</small>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer border-0">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn btn-theme-mindmaps" id="save-node-style">Save Changes</button>
                    </div>
                </div>
            </div>
        `;

        document.body.appendChild(modal);
        
        // Initialize Bootstrap modal
        const bsModal = new bootstrap.Modal(modal);
        bsModal.show();

        // Update size display
        const sizeInput = modal.querySelector('#node-size-input');
        const sizeValue = modal.querySelector('#size-value');
        sizeInput.addEventListener('input', () => {
            sizeValue.textContent = sizeInput.value;
        });

        // Handle save
        modal.querySelector('#save-node-style').addEventListener('click', () => {
            const styleUpdates = {
                type: modal.querySelector('#node-type-select').value,
                color: modal.querySelector('#node-color-input').value,
                shape: modal.querySelector('#node-shape-select').value,
                size: parseInt(sizeInput.value)
            };

            // Set icon based on node type
            if (styleUpdates.type) {
                styleUpdates.icon = this.getNodeTypeIcon(styleUpdates.type);
            }

            this.updateNodeStyle(nodeId, styleUpdates);
            bsModal.hide();
        });

        // Cleanup on hide
        modal.addEventListener('hidden.bs.modal', () => {
            document.body.removeChild(modal);
        });
    }

    showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `alert alert-${type === 'error' ? 'danger' : type === 'success' ? 'success' : 'info'} alert-dismissible fade show position-fixed`;
        notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 250px;';
        notification.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

        document.body.appendChild(notification);
        
        // Auto-dismiss after 3 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 3000);
    }

    exportAsJson() {
        if (!this.cy || !this.options.mapId) {
            console.error('Cytoscape instance or map ID not available');
            return;
        }

        window.open(`${this.options.apiBase}/ExportAsJson?id=${this.options.mapId}`, '_blank');
    }

    showImportDialog() {
        const modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.innerHTML = `
            <div class="modal-dialog">
                <div class="modal-content card border-theme-mindmaps">
                    <div class="modal-header bg-theme-mindmaps text-on-theme-mindmaps border-0">
                        <h5 class="modal-title">Import Mind Map</h5>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <div class="mb-3">
                            <label class="form-label text-muted small fw-bold">SELECT JSON FILE</label>
                            <input type="file" id="import-file-input" class="form-control border-theme-mindmaps" accept=".json">
                        </div>
                        <div class="mb-3">
                            <label class="form-label text-muted small fw-bold">CONTEXT</label>
                            <select id="import-context-select" class="form-select border-theme-mindmaps">
                                <option value="">Select context...</option>
                            </select>
                        </div>
                    </div>
                    <div class="modal-footer border-0">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn btn-theme-mindmaps" id="import-mindmap">Import</button>
                    </div>
                </div>
            </div>
        `;

        document.body.appendChild(modal);
        
        // Initialize Bootstrap modal
        const bsModal = new bootstrap.Modal(modal);
        bsModal.show();

        // Load contexts
        this.loadContextsForImport(modal);

        // Handle import
        modal.querySelector('#import-mindmap').addEventListener('click', () => {
            const fileInput = modal.querySelector('#import-file-input');
            const contextSelect = modal.querySelector('#import-context-select');
            
            if (!fileInput.files[0]) {
                this.showNotification('Please select a file', 'error');
                return;
            }
            
            if (!contextSelect.value) {
                this.showNotification('Please select a context', 'error');
                return;
            }

            this.importMindMap(fileInput.files[0], contextSelect.value);
            bsModal.hide();
        });

        // Cleanup on hide
        modal.addEventListener('hidden.bs.modal', () => {
            document.body.removeChild(modal);
        });
    }

    async loadContextsForImport(modal) {
        try {
            const response = await fetch('/Contexts/ApiGetContexts');
            if (response.ok) {
                const contexts = await response.json();
                const select = modal.querySelector('#import-context-select');
                contexts.forEach(context => {
                    const option = document.createElement('option');
                    option.value = context.id;
                    option.textContent = context.name;
                    select.appendChild(option);
                });
            }
        } catch (error) {
            console.error('Error loading contexts:', error);
        }
    }

    async importMindMap(file, projectId) {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('projectId', projectId);

        try {
            const response = await fetch(`${this.options.apiBase}/ImportFromJson`, {
                method: 'POST',
                body: formData
            });

            if (response.ok) {
                this.showNotification('Mind map imported successfully', 'success');
                // Redirect to the new mind map
                const result = await response.text();
                if (result.includes('redirect')) {
                    window.location.reload();
                }
            } else {
                throw new Error('Import failed');
            }
        } catch (error) {
            console.error('Error importing mind map:', error);
            this.showNotification('Failed to import mind map', 'error');
        }
    }
}

// Initialize mind map when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    const container = document.getElementById('mind-map-container');
    if (container) {
        const projectId = container.dataset.projectId || '';
        const mapId = container.dataset.mapId || '';
        const apiBase = container.dataset.apiBase || '/MindMap';
        const mindMap = new CompostMindMap('mind-map-container', { projectId, mapId, apiBase });

        window.compostMindMap = mindMap;

        // Load node templates
        mindMap.loadNodeTemplates();

        // Existing event handlers
        document.getElementById('fit-view-btn')?.addEventListener('click', () => mindMap.fitToView());
        document.getElementById('spread-nodes-btn')?.addEventListener('click', () => mindMap.spreadNodes());
        document.getElementById('export-image-btn')?.addEventListener('click', () => mindMap.exportAsImage());

        // New event handlers
        document.getElementById('export-json-btn')?.addEventListener('click', () => mindMap.exportAsJson());
        document.getElementById('import-btn')?.addEventListener('click', () => mindMap.showImportDialog());

        // Layout buttons
        document.getElementById('layout-radial-btn')?.addEventListener('click', () => mindMap.applyLayout('radial'));
        document.getElementById('layout-grid-btn')?.addEventListener('click', () => mindMap.applyLayout('grid'));
        document.getElementById('layout-hierarchical-btn')?.addEventListener('click', () => mindMap.applyLayout('hierarchical'));
        document.getElementById('layout-circular-btn')?.addEventListener('click', () => mindMap.applyLayout('circular'));
        document.getElementById('layout-force-btn')?.addEventListener('click', () => mindMap.applyLayout('force'));

        // Template buttons
        document.querySelectorAll('[data-template]').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const templateId = e.target.dataset.template;
                mindMap.createNodeFromTemplate(templateId);
            });
        });

        // Add context menu for node styling
        if (mindMap.cy) {
            mindMap.cy.on('cxttap', (event) => {
                const target = event.target;
                // Check if target is a node (not the core/background)
                if (target !== mindMap.cy && target.isNode && target.isNode()) {
                    // Create context menu
                    const contextMenu = document.createElement('div');
                    contextMenu.className = 'dropdown-menu show';
                    contextMenu.style.cssText = 'position: absolute; left: ' + event.renderedPosition.x + 'px; top: ' + event.renderedPosition.y + 'px; z-index: 9999;';
                    contextMenu.innerHTML = `
                        <a class="dropdown-item" href="#" data-action="style">
                            <i class="fas fa-palette me-2"></i>Style Node
                        </a>
                        <a class="dropdown-item" href="#" data-action="edit">
                            <i class="fas fa-edit me-2"></i>Edit Node
                        </a>
                        <a class="dropdown-item" href="#" data-action="promote">
                            <i class="fas fa-arrow-up me-2"></i>Promote to Tree
                        </a>
                        <a class="dropdown-item" href="#" data-action="delete">
                            <i class="fas fa-trash me-2"></i>Delete Node
                        </a>
                    `;

                    document.body.appendChild(contextMenu);

                    // Handle menu clicks
                    contextMenu.addEventListener('click', (e) => {
                        e.preventDefault();
                        const action = e.target.dataset.action;
                        
                        switch (action) {
                            case 'style':
                                mindMap.showNodeStyleDialog(target.id());
                                break;
                            case 'edit':
                                // Trigger edit functionality
                                target.trigger('doubletap');
                                break;
                            case 'promote':
                                // Trigger promotion
                                mindMap.promoteNode(target.id());
                                break;
                            case 'delete':
                                // Trigger delete
                                mindMap.deleteNode(target.id());
                                break;
                        }

                        document.body.removeChild(contextMenu);
                    });

                    // Close menu when clicking elsewhere
                    setTimeout(() => {
                        document.addEventListener('click', function closeMenu() {
                            if (contextMenu.parentNode) {
                                document.body.removeChild(contextMenu);
                            }
                            document.removeEventListener('click', closeMenu);
                        });
                    }, 100);
                }
            });
        }
    }
});
