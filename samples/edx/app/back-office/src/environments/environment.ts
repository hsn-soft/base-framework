/** The addresses of the Edx hosts for the environment the application is built for (`ng build --configuration <environment>` replaces this file by environment.<environment>.ts). This is `development`: the hosts run on this machine (run-all.sh). */
export const environment = {
  name: 'development',
  /** The gateway of the Edx system: the identity, auth and invoice services are reached through it (REST, JSON). */
  apiBase: 'http://localhost:5600',
  /** The firm invoice integration (what customers send in) and the GİB invoice integration (the center's side) are not behind the gateway: the back office calls their REST hosts directly. */
  firmInvoiceBase: 'http://localhost:5510',
  gibInvoiceBase: 'http://localhost:5540',
};
