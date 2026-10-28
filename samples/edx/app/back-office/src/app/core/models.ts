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

/** Where the envelope of an invoice is (a summary: the envelope itself is the GİB invoice integration's). */
export type EnvelopeState = 'None' | 'ToSend' | 'Sent' | 'Delivered' | 'Refused' | 'Failed';

/** One step in the life of an invoice: its status changed, or its envelope did. */
export interface InvoiceHistoryEntry {
  kind: 'Status' | 'Envelope';
  value: string;
  at: string;
  by?: string | null;
  note?: string | null;
}

/** The answer of the receiver to a commercial invoice (the shape only). */
export interface InvoiceResponse {
  kind: 'Accepted' | 'Rejected';
  documentNumber: string;
  documentUuid: string;
  at: string;
  note?: string | null;
  envelopeId?: string | null;
  envelopeState?: string | null;
  envelopeStatusCode?: number | null;
  envelopeStatusText?: string | null;
  envelopeStatusAt?: string | null;
}

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
  profile?: string | null;
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
  envelopeId?: string | null;
  envelopeState: EnvelopeState;
  envelopeStatusCode?: number | null;
  envelopeStatusText?: string | null;
  envelopeNote?: string | null;
  envelopeStatusAt?: string | null;
  history: InvoiceHistoryEntry[];
  responses: InvoiceResponse[];
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

/** The model of an invoice, as the create page sends it (the same one a customer sends through the API). The supplier is the tenant itself. */
export interface InvoiceModel {
  invoiceId: string;
  invoiceNumber: string;
  issueDate: string;
  currency: string;
  customerName: string;
  customerTaxId: string;
  customerTaxOffice?: string | null;
  customerDistrict: string;
  customerCity: string;
  customerCountry?: string | null;
  invoiceProfile?: 'TEMELFATURA' | 'TICARIFATURA';
  lines: { description: string; quantity: number; unitCode?: string | null; unitPrice: number; vatRate: number }[];
}

/** An invoice that was issued to the tenant (it came in at its post box). */
export interface PurchaseInvoice {
  id: string;
  invoiceUuid: string;
  envelopeId: string;
  profile: string;
  number: string;
  issueDate: string;
  currency: string;
  supplierRegisterNumber: string;
  supplierName: string;
  customerRegisterNumber: string;
  customerName: string;
  lineCount: number;
  taxExclusiveAmount?: number | null;
  taxInclusiveAmount?: number | null;
  payableAmount?: number | null;
  receivedAt: string;
  /** What the center says of the envelope the invoice came in (1200, 1220, 1300...). */
  envelopeStatusCode: number;
  envelopeStatusText: string;
  envelopeStatusAt: string;
  history: InvoiceHistoryEntry[];
  responses: InvoiceResponse[];
}

/** An envelope the integration service handed to the center. */
export interface OutboundEnvelope {
  id: string;
  envelopeId: string;
  kind: 'Invoice' | 'SystemResponse';
  state: 'ToSend' | 'Sent' | 'Delivered' | 'Refused' | 'Failed';
  statusCode: number;
  statusDescription: string;
  failureStage?: string | null;
  failureReason?: string | null;
  senderAlias: string;
  receiverAlias: string;
  receiverRegisterNumber: string;
  receiverTitle: string;
  invoiceNumber?: string | null;
  referenceEnvelopeId?: string | null;
  sendCount: number;
  createdAt: string;
  statusChangedAt: string;
  history: EnvelopeEvent[];
}

/** One thing that happened to an envelope (a code of the center, or what the integration did). */
export interface EnvelopeEvent {
  at: string;
  code: number;
  text: string;
  source: 'Local' | 'Send' | 'Center' | 'Poll' | 'PostBox';
  note?: string | null;
}

/** An envelope the center delivered to the post box of the tenant. */
export interface InboundEnvelope {
  id: string;
  envelopeId: string;
  state: 'Queued' | 'Accepted' | 'Refused' | 'Applied' | 'Ignored';
  statusCode: number;
  statusDescription: string;
  documentType?: string | null;
  senderAlias?: string | null;
  senderRegisterNumber?: string | null;
  senderTitle?: string | null;
  receiverAlias?: string | null;
  invoiceNumber?: string | null;
  referenceEnvelopeId?: string | null;
  receivedAt: string;
  processedAt?: string | null;
  centerStatusCode?: number | null;
  centerStatusDescription?: string | null;
  centerStatusAt?: string | null;
  history: EnvelopeEvent[];
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
