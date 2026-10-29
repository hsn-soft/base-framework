/** The gateway of the Edx system: the identity, auth and invoice services are reached through it (REST, JSON). Change it for another environment. */
export const API_BASE = 'http://localhost:5600';

/** The firm invoice integration (what customers send in) and the GİB invoice integration (the center's side) are not behind the gateway: the back office calls their REST hosts directly. */
export const FIRM_INVOICE_BASE = 'http://localhost:5510';
export const GIB_INVOICE_BASE = 'http://localhost:5540';

/**
 * The address of a call. A path is written as `service/v1/...` (the way the gateway names it); the two integrations answer on their own address and under the routes of their controllers
 * (`api/<service>/v1/commercial/...`, and `api/firm-invoice-service/v1/back-office/...` for the pages of the back office).
 */
export function urlOf(path: string): string {
  if (path.startsWith('firm-invoice-service/v1/back-office/')) {
    return `${FIRM_INVOICE_BASE}/api/${path}`;
  }
  if (path.startsWith('firm-invoice-service/v1/')) {
    return `${FIRM_INVOICE_BASE}/api/firm-invoice-service/v1/commercial/${path.slice('firm-invoice-service/v1/'.length)}`;
  }
  if (path.startsWith('integration-service/v1/')) {
    return `${GIB_INVOICE_BASE}/api/integration-service/v1/commercial/${path.slice('integration-service/v1/'.length)}`;
  }
  return `${API_BASE}/${path}`;
}
