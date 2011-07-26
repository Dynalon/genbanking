%module AB
%{
        /* Includes the header in the wrapper code */
        #include "aqbanking/banking.h"
        #include "gwenhywfar/gwenhywfar.h"
        #include "gwenhywfar/gui_be.h"       
%}
/* define the common *_t types into basic C types so swig can marshal them into 
   c# primitve types.
  
  TODO: this may impose compatibility issues with non-x86 ISAs
*/

typedef unsigned int uint32_t;
typedef signed int int32_t;

%include "gwenhywfar/gwenhywfarapi.h"
%include "gwenhywfar/cgui.h"
%include "gwenhywfar/list1.h" 
%include "gwenhywfar/list2.h" 
%include "gwenhywfar/stringlist.h" 
%include "gwenhywfar/gwentime.h" 
%include "gwenhywfar/inherit.h"
%include "gwenhywfar/gui.h"
%include "gwenhywfar/gui_be.h"
%include "gwenhywfar/error.h"

%include "aqbanking/error.h"
%include "aqbanking/abgui.h"
%include "aqbanking/banking.h"
%include "aqbanking/job.h"
%include "aqbanking/jobgetbalance.h"
%include "aqbanking/jobgettransactions.h"
%include "aqbanking/user.h"
%include "aqbanking/account.h"
%include "aqbanking/banking_ob.h"
%include "aqbanking/imexporter.h"
%include "aqbanking/accstatus.h"
%include "aqbanking/balance.h"
%include "aqbanking/value.h"
%include "aqbanking/transaction.h"
