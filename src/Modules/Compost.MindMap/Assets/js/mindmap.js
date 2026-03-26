/**
 * Compost Mind Map - Interactive mind map using Cytoscape.js
 * Handles drag-drop, zoom, pan, and node management
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
        this.initialize();
    }

    initialize() {
        // Initialize Cytoscape
        this.cy = cytoscape({
            container: document.getElementById(this.containerId),
            
            style: [
                {
                    selector: 'node',
                    style: {
                        'background-color': 'data(color)',
                        'label': 'data(label)',
                        'width': '60px',
                        'height': '60px',
                        'text-valign': 'center',
                        'text-halign': 'center',
                        'font-size': '12px',
                        'text-wrap': 'wrap',
                        'text-max-width': '100px',
                        'border-width': 2,
                        'border-color': '#666'
                    }
                },
                {
                    selector: 'node[type="Requirement"]',
                    style: {
                        'background-color': '#2E7D32',
                        'shape': 'round-rectangle'
                    }
                },
                {
                    selector: 'node[type="Action"]',
                    style: {
                        'background-color': '#1976D2',
                        'shape': 'diamond'
                    }
                },
                {
                    selector: 'node[type="Question"]',
                    style: {
                        'background-color': '#F57C00',
                        'shape': 'ellipse'
                    }
                },
                {
                    selector: 'node[type="Decision"]',
                    style: {
                        'background-color': '#7B1FA2',
                        'shape': 'hexagon'
                    }
                },
                {
                    selector: 'node.promoted',
                    style: {
                        'border-color': '#FFD700',
                        'border-width': 4
                    }
                },
                {
                    selector: 'node:selected',
                    style: {
                        'border-color': '#FF4081',
                        'border-width': 4
                    }
                },
                {
                    selector: 'edge',
                    style: {
                        'width': 2,
                        'line-color': '#999',
                        'target-arrow-color': '#999',
                        'target-arrow-shape': 'triangle',
                        'curve-style': 'bezier'
                    }
                }
            ],
            
            layout: {
                name: 'preset'
            },
            
            // Enable pan and zoom
            wheelSensitivity: 0.2,
            minZoom: 0.5,
            maxZoom: 2
        });

        // Set up event handlers
        this.setupEventHandlers();
    }

    setupEventHandlers() {
        if (!this.options.editable) return;

        // Double-click to create node
        this.cy.on('dbltap', (event) => {
            if (event.target === this.cy) {
                const pos = event.position;
                this.createNode(pos.x, pos.y);
            }
        });

        // Single click to select/edit
        this.cy.on('tap', 'node', (event) => {
            const node = event.target;
            this.selectNode(node);
        });

        // Drag end - save position
        this.cy.on('dragfree', 'node', (event) => {
            const node = event.target;
            this.saveNodePosition(node);
        });

        // Project menu (right-click)
        this.cy.on('cxttap', 'node', (event) => {
            const node = event.target;
            this.showContextMenu(node, event.renderedPosition);
        });
    }

    async createNode(x, y, data = {}) {
        const nodeId = data.id || this.generateId();
        
        const node = this.cy.add({
            group: 'nodes',
            data: {
                id: nodeId,
                label: data.label || 'New Node',
                type: data.type || 'Idea',
                color: data.color || '#81C784'
            },
            position: { x, y }
        });

        // Save to backend
        await this.saveNode(node);
        
        return node;
    }

    async saveNode(node) {
        const nodeData = {
            id: node.id(),
            label: node.data('label'),
            title: node.data('label'),
            content: node.data('content') || '',
            nodeType: node.data('type'),
            positionX: node.position('x'),
            positionY: node.position('y'),
            color: node.data('color'),
            parentId: node.data('parentId') || null
        };

        const url = this.options.mapId
            ? `${this.options.apiBase}/ApiUpdateNode?mapId=${encodeURIComponent(this.options.mapId)}`
            : '/api/mindmap/nodes';
        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || '' },
                body: JSON.stringify(nodeData)
            });
            if (!response.ok) console.error('Failed to save node');
        } catch (error) {
            console.error('Error saving node:', error);
        }
    }

    async saveNodePosition(node) {
        const url = this.options.mapId
            ? `${this.options.apiBase}/ApiUpdateNodePosition?mapId=${encodeURIComponent(this.options.mapId)}&nodeId=${encodeURIComponent(node.id())}`
            : `/api/mindmap/nodes/${node.id()}/position`;
        try {
            await fetch(url, {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ x: node.position('x'), y: node.position('y') })
            });
        } catch (error) {
            console.error('Error saving node position:', error);
        }
    }

    selectNode(node) {
        // Show details panel
        this.showNodeDetails(node);
    }

    showNodeDetails(node) {
        const panel = document.getElementById('node-details-panel');
        if (!panel) return;

        // Check if node has children for structure promotion eligibility
        const hasChildren = this.getNodeChildren(node).length > 0;

        panel.innerHTML = `
            <h3>Node Details</h3>
            <div class="form-group">
                <label>Title</label>
                <input type="text" id="node-title" value="${node.data('label')}" class="form-control" />
            </div>
            <div class="form-group">
                <label>Type</label>
                <select id="node-type" class="form-control">
                    <option value="Idea">Idea</option>
                    <option value="Requirement">Requirement</option>
                    <option value="Action">Action</option>
                    <option value="Question">Question</option>
                    <option value="Decision">Decision</option>
                    <option value="Risk">Risk</option>
                </select>
            </div>
            <div class="form-group">
                <label>Content</label>
                <textarea id="node-content" class="form-control" rows="4">${node.data('content') || ''}</textarea>
            </div>
            <div class="form-group">
                <label>Status</label>
                <div class="alert alert-info">
                    <i class="fas fa-info-circle"></i> 
                    ${node.data('promoted') ? 'This node has been promoted' : 'Ready for promotion'}
                    ${hasChildren ? '<br><i class="fas fa-users"></i> Has children - eligible for Structure promotion' : ''}
                </div>
            </div>
            <div class="btn-group-vertical">
                <button id="save-node-btn" class="btn btn-primary btn-sm mb-2">
                    <i class="fas fa-save"></i> Save Changes
                </button>
                ${!node.data('promoted') ? `
                    <button id="promote-tree-btn" class="btn btn-success btn-sm mb-2">
                        <i class="fas fa-arrow-up"></i> Promote to Tree
                    </button>
                    ${hasChildren ? `
                        <button id="promote-structure-btn" class="btn btn-warning btn-sm mb-2">
                            <i class="fas fa-sitemap"></i> Promote to Structure
                        </button>
                    ` : ''}
                ` : `
                    <div class="alert alert-success">
                        <i class="fas fa-check"></i> Already promoted to ${node.data('promotionType') || 'Tree'}
                    </div>
                `}
                <button id="delete-node-btn" class="btn btn-danger btn-sm">
                    <i class="fas fa-trash"></i> Delete
                </button>
            </div>
        `;

        // Set current type
        document.getElementById('node-type').value = node.data('type');

        // Event handlers
        document.getElementById('save-node-btn').addEventListener('click', () => {
            this.updateNode(node);
        });

        const promoteTreeBtn = document.getElementById('promote-tree-btn');
        if (promoteTreeBtn) {
            promoteTreeBtn.addEventListener('click', () => {
                this.promoteNodeToTree(node);
            });
        }

        const promoteStructureBtn = document.getElementById('promote-structure-btn');
        if (promoteStructureBtn) {
            promoteStructureBtn.addEventListener('click', () => {
                this.promoteNodeToStructure(node);
            });
        }

        document.getElementById('delete-node-btn').addEventListener('click', () => {
            this.deleteNode(node);
        });

        panel.style.display = 'block';
    }

    async updateNode(node) {
        const title = document.getElementById('node-title').value;
        const type = document.getElementById('node-type').value;
        const content = document.getElementById('node-content').value;

        node.data('label', title);
        node.data('type', type);
        node.data('content', content);

        await this.saveNode(node);
    }

    getNodeChildren(node) {
        return this.cy.edges().filter(edge => edge.data('source') === node.id())
            .map(edge => this.cy.getElementById(edge.data('target')));
    }

    async promoteNodeToTree(node) {
        const url = this.options.mapId
            ? `${this.options.apiBase}/ApiPromoteNode?mapId=${encodeURIComponent(this.options.mapId)}&nodeId=${encodeURIComponent(node.id())}`
            : `/api/mindmap/nodes/${node.id()}/promote`;
        
        try {
            const response = await fetch(url, { method: 'POST' });
            if (response.ok) {
                const data = await response.json();
                node.addClass('promoted');
                node.data('promoted', true);
                node.data('promotionType', 'Tree');
                node.data('treeNodeId', data.treeNodeId);
                
                // Show success message
                this.showNotification('Node promoted to Tree successfully!', 'success');
                
                // Refresh node details panel
                this.selectNode(node);
                
                // Optionally navigate to refinement page
                if (data.url && confirm('Navigate to tree refinement page?')) {
                    window.location.href = data.url;
                }
            } else {
                const error = await response.text();
                this.showNotification(`Promote failed: ${error}`, 'error');
            }
        } catch (error) {
            console.error('Error promoting node to tree:', error);
            this.showNotification('Error promoting node to tree', 'error');
        }
    }

    async promoteNodeToStructure(node) {
        const url = this.options.mapId
            ? `${this.options.apiBase}/ApiPromoteToStructure?mapId=${encodeURIComponent(this.options.mapId)}&nodeId=${encodeURIComponent(node.id())}`
            : `/api/mindmap/nodes/${node.id()}/promote-structure`;
        
        try {
            const response = await fetch(url, { method: 'POST' });
            if (response.ok) {
                const data = await response.json();
                node.addClass('promoted');
                node.data('promoted', true);
                node.data('promotionType', 'Structure');
                node.data('structureId', data.structureId);
                
                // Show success message
                this.showNotification(data.message || 'Node promoted to Structure successfully!', 'success');
                
                // Refresh node details panel
                this.selectNode(node);
                
                // Optionally navigate to structure page
                if (confirm('Navigate to structure management page?')) {
                    window.location.href = `/Structure/Detail/${data.structureId}`;
                }
            } else {
                const error = await response.text();
                this.showNotification(`Structure promote failed: ${error}`, 'error');
            }
        } catch (error) {
            console.error('Error promoting node to structure:', error);
            this.showNotification('Error promoting node to structure', 'error');
        }
    }

    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
        notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
        notification.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        document.body.appendChild(notification);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 5000);
    }

    async deleteNode(node) {
        if (!confirm('Delete this node?')) return;
        const url = this.options.mapId
            ? `${this.options.apiBase}/ApiDeleteNode?mapId=${encodeURIComponent(this.options.mapId)}&nodeId=${encodeURIComponent(node.id())}`
            : `/api/mindmap/nodes/${node.id()}`;
        try {
            await fetch(url, { method: 'DELETE' });
            this.cy.remove(node);
            const panel = document.getElementById('node-details-panel');
            if (panel) panel.style.display = 'none';
        } catch (error) {
            console.error('Error deleting node:', error);
        }
    }

    showContextMenu(node, position) {
        // Check if node has children for structure promotion eligibility
        const hasChildren = this.getNodeChildren(node).length > 0;
        const isPromoted = node.data('promoted');

        // Create context menu
        const menu = document.createElement('div');
        menu.className = 'mind-map-context-menu';
        menu.style.position = 'absolute';
        menu.style.left = position.x + 'px';
        menu.style.top = position.y + 'px';
        menu.style.cssText += 'background: #2a2a2a; border: 1px solid #444; border-radius: 4px; padding: 8px 0; box-shadow: 0 4px 12px rgba(0,0,0,0.5); z-index: 1000;';
        
        menu.innerHTML = `
            <div class="context-menu-item" data-action="edit" style="padding: 8px 16px; cursor: pointer; color: #fff; border-bottom: 1px solid #444;">
                <i class="fas fa-edit"></i> Edit
            </div>
            ${!isPromoted ? `
                <div class="context-menu-item" data-action="promote-tree" style="padding: 8px 16px; cursor: pointer; color: #4caf50; border-bottom: 1px solid #444;">
                    <i class="fas fa-arrow-up"></i> Promote to Tree
                </div>
                ${hasChildren ? `
                    <div class="context-menu-item" data-action="promote-structure" style="padding: 8px 16px; cursor: pointer; color: #ff9800; border-bottom: 1px solid #444;">
                        <i class="fas fa-sitemap"></i> Promote to Structure
                    </div>
                ` : ''}
            ` : `
                <div class="context-menu-item" style="padding: 8px 16px; cursor: pointer; color: #666; border-bottom: 1px solid #444;">
                    <i class="fas fa-check"></i> Already promoted to ${node.data('promotionType') || 'Tree'}
                </div>
            `}
            <div class="context-menu-item" data-action="connect" style="padding: 8px 16px; cursor: pointer; color: #2196f3; border-bottom: 1px solid #444;">
                <i class="fas fa-link"></i> Connect to...
            </div>
            <div class="context-menu-item" data-action="delete" style="padding: 8px 16px; cursor: pointer; color: #f44336;">
                <i class="fas fa-trash"></i> Delete
            </div>
        `;

        document.body.appendChild(menu);

        // Add hover effects
        menu.querySelectorAll('.context-menu-item').forEach(item => {
            item.addEventListener('mouseenter', () => {
                item.style.backgroundColor = '#444';
            });
            item.addEventListener('mouseleave', () => {
                item.style.backgroundColor = 'transparent';
            });
        });

        // Handle menu clicks
        menu.addEventListener('click', (e) => {
            const action = e.target.closest('.context-menu-item')?.dataset.action;
            if (action === 'edit') this.selectNode(node);
            if (action === 'promote-tree') this.promoteNodeToTree(node);
            if (action === 'promote-structure') this.promoteNodeToStructure(node);
            if (action === 'delete') this.deleteNode(node);
            menu.remove();
        });

        // Remove on outside click
        setTimeout(() => {
            document.addEventListener('click', () => menu.remove(), { once: true });
        }, 100);
    }

    async loadNodesForMap(mapId) {
        if (!mapId) return;
        try {
            const response = await fetch(`${this.options.apiBase}/ApiMap/${encodeURIComponent(mapId)}`);
            if (!response.ok) return;
            const data = await response.json();
            const nodes = data.nodes || [];
            nodes.forEach(nodeData => {
                const isPromoted = nodeData.isPromoted || nodeData.isPromotedToTree || nodeData.isPromotedToStructure;
                const promotionType = nodeData.isPromotedToStructure ? 'Structure' : (nodeData.isPromotedToTree ? 'Tree' : null);
                
                this.cy.add({
                    group: 'nodes',
                    data: {
                        id: nodeData.id,
                        label: nodeData.text || nodeData.title,
                        type: nodeData.nodeType || 'Idea',
                        color: nodeData.color || '#81C784',
                        content: nodeData.notes || nodeData.sourceText,
                        promoted: isPromoted,
                        promotionType: promotionType,
                        treeNodeId: nodeData.treeNodeId,
                        structureId: nodeData.structureNodeId,
                        status: nodeData.status
                    },
                    position: { x: nodeData.positionX || 0, y: nodeData.positionY || 0 },
                    classes: isPromoted ? 'promoted' : ''
                });
                if (nodeData.parentId) {
                    this.cy.add({
                        group: 'edges',
                        data: { source: nodeData.parentId, target: nodeData.id }
                    });
                }
            });
        } catch (error) {
            console.error('Error loading map:', error);
            this.showNotification('Error loading mind map', 'error');
        }
    }

    async loadNodesForContext(projectId) {
        try {
            const response = await fetch(`/api/mindmap/contexts/${projectId}/nodes`);
            const nodes = await response.json();
            nodes.forEach(nodeData => {
                this.cy.add({
                    group: 'nodes',
                    data: {
                        id: nodeData.id,
                        label: nodeData.title,
                        type: nodeData.nodeType,
                        color: nodeData.color || '#81C784',
                        content: nodeData.content,
                        promoted: nodeData.isPromotedToTree
                    },
                    position: { x: nodeData.positionX, y: nodeData.positionY },
                    classes: nodeData.isPromotedToTree ? 'promoted' : ''
                });
                if (nodeData.parentNodeId) {
                    this.cy.add({ group: 'edges', data: { source: nodeData.parentNodeId, target: nodeData.id } });
                }
            });
        } catch (error) {
            console.error('Error loading nodes:', error);
        }
    }

    generateId() {
        return 'node_' + Math.random().toString(36).substr(2, 9);
    }

    // Export as image
    exportAsImage() {
        const png = this.cy.png({ full: true, scale: 2 });
        const link = document.createElement('a');
        link.href = png;
        link.download = 'mindmap.png';
        link.click();
    }

    // Fit to view
    fitToView() {
        this.cy.fit(null, 50);
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

        if (mapId) {
            mindMap.loadNodesForMap(mapId);
        } else if (projectId) {
            mindMap.loadNodesForContext(projectId);
        }

        window.compostMindMap = mindMap;

        document.getElementById('fit-view-btn')?.addEventListener('click', () => mindMap.fitToView());
        document.getElementById('export-image-btn')?.addEventListener('click', () => mindMap.exportAsImage());
    }
});
