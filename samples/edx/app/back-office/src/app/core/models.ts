/** What every answer of the gateway is: the data in `payload`, and (for a failure) the messages and the code. */
export interface ApiResponse<T> {
  payload: T;
  statusCode: number;
  statusMessages?: string[];
  errorCode?: string;
  traceId?: string;
}

export interface Paged<T> {
  items: T[];
  dataTotalCount: number;
  pageTotalCount: number;
}

export interface PagedRequest {
  pageNumber: number;
  maxResultCount: number;
  searchText?: string;
  sortingText?: string;
}

export type PermissionRelation = 'Visibility' | 'Required' | 'Action' | 'Operation' | 'Constraint';
export type PermissionType = 'Page' | 'Endpoint' | 'Operation' | 'Constraint';

export interface MenuPermission {
  code: string;
  type: PermissionType;
  relation: PermissionRelation;
  granted: boolean;
}

export interface MenuItem {
  code: string;
  title: string;
  route?: string | null;
  icon?: string | null;
  nodeType: 'Caption' | 'Node' | 'Leaf';
  children: MenuItem[];
  permissions: MenuPermission[];
}

/** A permission the signed-in user has; a constraint carries its value. */
export interface Grant {
  code: string;
  type: PermissionType;
  value?: string | null;
}

export interface InvoiceLine {
  description: string;
  quantity: number;
  unitPrice?: number | null;
  vatRate: number;
  total?: number | null;
}

export type InvoiceStatus = 'Processing' | 'Failed' | 'DraftCreated' | 'Rejected' | 'Approved' | 'Signed';

/** One record of an invoice (it is sent again after a failure, and each time is a record). */
export interface Invoice {
  id: string;
  invoiceId: string;
  queueId: string;
  number: string;
  sourceType: string;
  channelType: string;
  status: InvoiceStatus;
  isLive: boolean;
  failureStage?: string | null;
  failureReason?: string | null;
  statusChangedAt: string;
  issueDate?: string | null;
  currency?: string | null;
  supplierName?: string | null;
  supplierTaxId?: string | null;
  customerName?: string | null;
  customerTaxId?: string | null;
  lines: InvoiceLine[];
  total?: number | null;
  rejectionReason?: string | null;
  tenantId: string;
}

export type QueueStatus = 'Queued' | 'DraftCreated' | 'Rejected' | 'Failed' | 'Completed';

export interface QueueItem {
  id: string;
  invoiceId: string;
  invoiceNumber: string;
  receivedBy: string;
  sourceType: string;
  channelType: string;
  status: QueueStatus;
  statusReason?: string | null;
  statusChangedAt: string;
  fileName: string;
  fileHash: string;
  sizeBytes: number;
  tenantId: string;
  createdAt: string;
}

/** The model of an invoice, as the create page sends it (the same one a customer sends through the API). */
export interface InvoiceModel {
  invoiceId: string;
  invoiceNumber: string;
  issueDate: string;
  currency: string;
  supplierName: string;
  supplierTaxId?: string | null;
  customerName: string;
  customerTaxId?: string | null;
  lines: { description: string; quantity: number; unitPrice: number; vatRate: number }[];
}

export interface InvoiceReport {
  id: string;
  name: string;
  periodStart: string;
  periodEnd: string;
  currency: string;
  invoiceCount: number;
  approvedCount: number;
  totalAmount?: number | null;
  generatedAt: string;
}

export interface User {
  id: string;
  userName: string;
  email: string;
  displayName: string;
  isActive: boolean;
  roles: string[];
  tenantId: string;
}

export interface Role {
  id: string;
  name: string;
  tenantId: string;
}

export interface Permission {
  id: string;
  code: string;
  type: PermissionType;
  name: string;
  isActive: boolean;
}
