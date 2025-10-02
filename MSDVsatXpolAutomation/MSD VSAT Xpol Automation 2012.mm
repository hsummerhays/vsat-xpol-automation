<map version="0.9.0">
<!-- To view this file, download free mind mapping software FreeMind from http://freemind.sourceforge.net -->
<node CREATED="1336674444295" ID="ID_1403554808" MODIFIED="1347312713694" TEXT="MSD VSAT Xpol Automation 2012">
<node CREATED="1336674737422" FOLDED="true" ID="ID_383952663" MODIFIED="1346969475291" POSITION="right" TEXT="Purpose">
<node CREATED="1336674741853" ID="ID_147162928" MODIFIED="1336674774309" TEXT="New uplink in Colorado needs way to do CW alignments"/>
<node CREATED="1336674777678" ID="ID_1452589920" MODIFIED="1336674796213" TEXT="Provide UI and spec A interface"/>
<node CREATED="1336674797111" ID="ID_534378593" MODIFIED="1336674831655" TEXT="Use single set of spec A&apos;s - scan all CWs every 5.5 secs"/>
<node CREATED="1337874302315" ID="ID_1165130420" MODIFIED="1337874354018" TEXT="Provide functionality for checking site alignment periodically on all sites after hours then write alerts to HAL as needed">
<node CREATED="1344555866440" ID="ID_1734461233" MODIFIED="1344555879824" TEXT="Very resource intensive when done manually"/>
<node CREATED="1344555880351" ID="ID_915862094" MODIFIED="1344555919805" TEXT="Echostar is getting impatient with our xpol interference"/>
</node>
</node>
<node CREATED="1336674875986" FOLDED="true" ID="ID_847807585" MODIFIED="1339614473643" POSITION="left" TEXT="Use existing software">
<icon BUILTIN="button_ok"/>
<node CREATED="1336674889211" ID="ID_1839127206" MODIFIED="1336675633857" TEXT="Have old code from 2004">
<icon BUILTIN="button_ok"/>
<node CREATED="1336675293364" ID="ID_1280965143" MODIFIED="1336675643529" TEXT="Is written in C#"/>
</node>
<node CREATED="1336675240178" ID="ID_783836216" MODIFIED="1336675638137" TEXT="Improved it with code xml docs and some refactoring">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1336675675692" ID="ID_138497087" MODIFIED="1336676251939" TEXT="Functional description">
<icon BUILTIN="button_ok"/>
<node CREATED="1336675687349" ID="ID_1295030314" MODIFIED="1336675720037" TEXT="Finds beacons on copol and xpol then records offsets"/>
<node CREATED="1336675724110" ID="ID_337994493" MODIFIED="1336675763839" TEXT="Uses offsets to look for single cw selected by user"/>
<node CREATED="1336675767553" ID="ID_392731123" MODIFIED="1336675799656" TEXT="Zooms in on cw and gives following">
<node CREATED="1336675800634" ID="ID_1349591911" MODIFIED="1336675820313" TEXT="Copol amplitude"/>
<node CREATED="1336675820802" ID="ID_324347462" MODIFIED="1336675828330" TEXT="Xpol amplitude"/>
<node CREATED="1336675829819" ID="ID_1317719483" MODIFIED="1336675833761" TEXT="Isolation value"/>
<node CREATED="1336675852460" ID="ID_328274077" MODIFIED="1336675861718" TEXT="Graph of copol"/>
<node CREATED="1336675862148" ID="ID_988746500" MODIFIED="1336675866626" TEXT="Graph of xpol"/>
</node>
</node>
<node CREATED="1336675357471" FOLDED="true" ID="ID_1217136789" MODIFIED="1339614450966" TEXT="Fix remaining problems">
<icon BUILTIN="button_ok"/>
<node CREATED="1336675367679" ID="ID_1820570020" MODIFIED="1336675400066" TEXT="See &quot;Xpol&quot; Outlook post item notes">
<node CREATED="1336675488077" ID="ID_633515441" MODIFIED="1337356183156" TEXT="Avoid incorrect beacon offsets">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1336675540375" ID="ID_1123825617" MODIFIED="1337356185283" TEXT="Fix so beacons are found every attempt">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1336675553615" ID="ID_1618731311" MODIFIED="1337356187089" TEXT="Use *OPC? command to always get clean data from spec A&apos;s">
<icon BUILTIN="button_ok"/>
</node>
</node>
</node>
</node>
<node CREATED="1336675869621" ID="ID_824231388" MODIFIED="1344611534175" POSITION="right" TEXT="New functionality">
<node CREATED="1336676571019" FOLDED="true" ID="ID_958427550" MODIFIED="1343324334025" TEXT="Windows service that talks to the Spec A&apos;s and ASP.net">
<icon BUILTIN="button_ok"/>
<node CREATED="1336675878341" ID="ID_489066358" MODIFIED="1336773448079" TEXT="Use beacon offsets to scan all active CWs">
<icon BUILTIN="button_cancel"/>
<node CREATED="1336773457592" ID="ID_1255773954" MODIFIED="1336773463915" TEXT="Use CW status instead"/>
</node>
<node CREATED="1336675959993" ID="ID_1543667570" MODIFIED="1343324320606" TEXT="Check beacons once every 24 hours">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1336675906582" ID="ID_577474348" MODIFIED="1336768298765" TEXT="Stop scanning if CW goes away?">
<icon BUILTIN="button_cancel"/>
</node>
<node CREATED="1336675914790" ID="ID_1696772620" MODIFIED="1343324316183" TEXT="Stop scanning if no response from client">
<icon BUILTIN="button_ok"/>
<node CREATED="1336676043184" ID="ID_829461730" MODIFIED="1336676075620" TEXT="Client will pole for data once per second"/>
<node CREATED="1336676095238" ID="ID_1470111114" MODIFIED="1336676124110" TEXT="Only need to scan when client asks">
<node CREATED="1336748698309" ID="ID_1500283374" MODIFIED="1336748718997" TEXT="If getting multiple requests, still only scan once per sec max"/>
<node CREATED="1336768273680" ID="ID_793352902" MODIFIED="1343324311239" TEXT="Can check queue at each request and add to queue if needed">
<icon BUILTIN="button_cancel"/>
</node>
</node>
</node>
<node CREATED="1336685509217" ID="ID_1221802361" MODIFIED="1343324329910" TEXT="Keep track of the following">
<icon BUILTIN="button_ok"/>
<node CREATED="1336685536975" ID="ID_195922904" MODIFIED="1336685545503" TEXT="Current CW state">
<node CREATED="1336685606499" ID="ID_1691345076" MODIFIED="1336685619277" TEXT="Start Search"/>
<node CREATED="1336685546487" ID="ID_133788579" MODIFIED="1336685679608" TEXT="Searching">
<arrowlink DESTINATION="ID_133788579" ENDARROW="Default" ENDINCLINATION="0;0;" ID="Arrow_ID_1303808861" STARTARROW="None" STARTINCLINATION="0;0;"/>
</node>
<node CREATED="1336685553718" ID="ID_389418610" MODIFIED="1336685555183" TEXT="Found">
<node CREATED="1336685657136" ID="ID_47468106" MODIFIED="1336685679607" TEXT="If lost">
<arrowlink DESTINATION="ID_133788579" ENDARROW="Default" ENDINCLINATION="46;0;" ID="Arrow_ID_5385021" STARTARROW="None" STARTINCLINATION="46;0;"/>
</node>
</node>
<node CREATED="1336685556342" ID="ID_990953664" MODIFIED="1336768385046" TEXT="Scanning Inactive"/>
</node>
<node CREATED="1336773572409" ID="ID_1219708623" MODIFIED="1336773589908" TEXT="copol amplitude"/>
<node CREATED="1336773590632" ID="ID_1465561165" MODIFIED="1336773597884" TEXT="xpol amplitude"/>
<node CREATED="1336773598768" ID="ID_861969216" MODIFIED="1336773601693" TEXT="isolation"/>
<node CREATED="1336773602391" ID="ID_212043397" MODIFIED="1336773613939" TEXT="SignalData"/>
</node>
</node>
<node CREATED="1336675999706" FOLDED="true" ID="ID_733668295" MODIFIED="1344557069923" TEXT="Web UI">
<icon BUILTIN="button_ok"/>
<node CREATED="1336676005306" ID="ID_68974578" MODIFIED="1336676011522" TEXT="List of CWs to look at"/>
<node CREATED="1336676012547" ID="ID_1288708503" MODIFIED="1336769010834" TEXT="Selecting one starts scanning process and shows detail"/>
<node CREATED="1336676134256" ID="ID_1669866405" MODIFIED="1336676148768" TEXT="Use AJAX to avoid full screen refresh"/>
<node CREATED="1336748382419" ID="ID_1281780526" MODIFIED="1344557062285" TEXT="Link into HAL">
<icon BUILTIN="button_ok"/>
<node CREATED="1336748544909" ID="ID_933771061" MODIFIED="1343324816626" TEXT="Write alerts right now if have access to centralsql">
<icon BUILTIN="button_cancel"/>
</node>
<node CREATED="1336748390814" ID="ID_1970083321" MODIFIED="1336773386186" TEXT="May want to consider using Remoting, but going to start with WCF to learn it">
<icon BUILTIN="button_cancel"/>
<node CREATED="1336750607298" ID="ID_460655788" MODIFIED="1336750635627" TEXT="Won&apos;t need Remoting since WCF can act like web service"/>
</node>
<node CREATED="1336748427956" ID="ID_1908771711" MODIFIED="1336773400873" TEXT="Could be listed under Monitoring">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1336748454435" ID="ID_703916830" MODIFIED="1342107395336" TEXT="Could use a HAL compatible DB">
<arrowlink DESTINATION="ID_562963766" ENDARROW="Default" ENDINCLINATION="888;0;" ID="Arrow_ID_1303555122" STARTARROW="None" STARTINCLINATION="888;0;"/>
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1336748483393" ID="ID_489501895" MODIFIED="1342107420583" TEXT="Could have separate monitor points for each VSAT network">
<icon BUILTIN="button_cancel"/>
</node>
<node CREATED="1336749028418" ID="ID_562963766" MODIFIED="1343324802490" TEXT="Consider what DB needs will be">
<icon BUILTIN="button_ok"/>
<node CREATED="1336751828212" FOLDED="true" ID="ID_1372851959" MODIFIED="1343324438920" TEXT="List of CWs with their status">
<arrowlink DESTINATION="ID_1071354779" ENDARROW="Default" ENDINCLINATION="177;0;" ID="Arrow_ID_542074047" STARTARROW="None" STARTINCLINATION="177;0;"/>
<icon BUILTIN="button_ok"/>
<node CREATED="1336751842307" ID="ID_1299132590" MODIFIED="1342107263720" TEXT="This could be the tSiteInfo table">
<icon BUILTIN="button_cancel"/>
</node>
<node CREATED="1342107246267" ID="ID_1806166251" MODIFIED="1342107258337" TEXT="Created new tSignalSlots table">
<node CREATED="1342107287560" ID="ID_1429680262" MODIFIED="1342107305542" TEXT="Status not stored here since HAL retrieves from RMP">
<node CREATED="1342107308519" ID="ID_1944249963" MODIFIED="1342107317286" TEXT="HAL could write status back to DB"/>
</node>
</node>
</node>
<node CREATED="1336751999498" ID="ID_1864202955" MODIFIED="1343324766620" TEXT="CW events (e.g. found, etc.)">
<icon BUILTIN="button_cancel"/>
<node CREATED="1336752015273" ID="ID_1172158727" MODIFIED="1336752023788" TEXT="May not be very useful"/>
</node>
<node CREATED="1336751890816" ID="ID_1584139975" MODIFIED="1336751974465" TEXT="List of RCSTs (could just add fields to Kenyon&apos;s table)"/>
</node>
</node>
</node>
<node CREATED="1336676171050" ID="ID_1372571278" MODIFIED="1344611811590" TEXT="Checking site alignment during off hours (VsatXpol Batches)">
<node CREATED="1344558095184" FOLDED="true" ID="ID_1037636803" MODIFIED="1346969320652" TEXT="Simple UI">
<icon BUILTIN="button_ok"/>
<node CREATED="1344558103128" FOLDED="true" ID="ID_394037205" MODIFIED="1344611544665" TEXT="Could use FileMaker 11">
<icon BUILTIN="button_cancel"/>
<node CREATED="1344558671999" ID="ID_1262998111" MODIFIED="1344558676321" TEXT="Easy UI"/>
<node CREATED="1344558676711" ID="ID_1232729791" MODIFIED="1344558693878" TEXT="Easy lookup"/>
<node CREATED="1344558681719" ID="ID_684147332" MODIFIED="1344558697766" TEXT="Easy export"/>
<node CREATED="1344558698678" ID="ID_1954946988" MODIFIED="1344558706349" TEXT="Users already know it"/>
<node CREATED="1344558706821" ID="ID_389538719" MODIFIED="1344558842678" TEXT="Many already have it installed - some need it though:">
<node CREATED="1344558796360" ID="ID_1013372587" MODIFIED="1344558802806" TEXT="Jason Davies"/>
<node CREATED="1344558803704" ID="ID_1782991423" MODIFIED="1344558808280" TEXT="Jason Warner"/>
<node CREATED="1344558808679" ID="ID_187889763" MODIFIED="1344558818623" TEXT="Andy Loader"/>
<node CREATED="1344558818959" ID="ID_989581700" MODIFIED="1344558829431" TEXT="David Frymire"/>
</node>
<node CREATED="1344558730684" ID="ID_914410481" MODIFIED="1344558912401" TEXT="Can talk to SQL DB directly (FileMaker 5 doesn&apos;t)">
<arrowlink DESTINATION="ID_1549576304" ENDARROW="Default" ENDINCLINATION="181;0;" ID="Arrow_ID_281700750" STARTARROW="None" STARTINCLINATION="181;0;"/>
</node>
</node>
<node CREATED="1344558258271" FOLDED="true" ID="ID_1097623130" MODIFIED="1344611579466" TEXT="Could use Servoy">
<icon BUILTIN="button_cancel"/>
<node CREATED="1344611550020" ID="ID_149305169" MODIFIED="1344611571940" TEXT="Too time consuming since don&apos;t have good framework in place yet."/>
</node>
<node CREATED="1344559978028" FOLDED="true" ID="ID_1661450053" MODIFIED="1346969256963" TEXT="Could use HAL with simple import">
<icon BUILTIN="button_ok"/>
<node CREATED="1344559991028" ID="ID_757683289" MODIFIED="1344560017508" TEXT="Show under monitoring"/>
<node CREATED="1344560017842" ID="ID_236817873" MODIFIED="1344560457626" TEXT="List">
<node CREATED="1344560458489" ID="ID_1048439200" MODIFIED="1344560462386" TEXT="List of batches"/>
</node>
<node CREATED="1344560437090" ID="ID_1883533982" MODIFIED="1344560454091" TEXT="Detail">
<node CREATED="1344560464449" ID="ID_92851904" MODIFIED="1344560588691" TEXT="Batch detail at top"/>
<node CREATED="1344560469720" ID="ID_69777943" MODIFIED="1344560583307" TEXT="List of RCSTs and readings at bottom"/>
<node CREATED="1344560487295" ID="ID_1086175935" MODIFIED="1344611680320" TEXT="Allow import of RCST macs">
<arrowlink DESTINATION="ID_1817300797" ENDARROW="Default" ENDINCLINATION="365;0;" ID="Arrow_ID_769139007" STARTARROW="None" STARTINCLINATION="365;0;"/>
<node CREATED="1344560596649" ID="ID_1383954039" MODIFIED="1344560610394" TEXT="Disable button if batch in progress or complete"/>
</node>
<node CREATED="1344560545084" ID="ID_846564828" MODIFIED="1346969249400" TEXT="Later could allow selection of RCSTs in an expanded section">
<arrowlink DESTINATION="ID_150596713" ENDARROW="Default" ENDINCLINATION="142;0;" ID="Arrow_ID_434650193" STARTARROW="None" STARTINCLINATION="142;0;"/>
<icon BUILTIN="button_cancel"/>
</node>
</node>
</node>
<node CREATED="1344558120231" FOLDED="true" ID="ID_1841666132" MODIFIED="1346969315485" TEXT="Need to do following">
<icon BUILTIN="button_ok"/>
<node CREATED="1344558131054" ID="ID_570526349" MODIFIED="1344560935588" TEXT="Show RCSTs by network and population">
<icon BUILTIN="button_cancel"/>
</node>
<node CREATED="1336676198611" ID="ID_396970690" MODIFIED="1343324635236" TEXT="Pause if user logs in">
<icon BUILTIN="button_cancel"/>
</node>
<node CREATED="1343324694522" FOLDED="true" ID="ID_1925630573" MODIFIED="1346969308182" TEXT="Allow user to select a batch of RCSTs to check and schedule a time">
<icon BUILTIN="button_ok"/>
<node CREATED="1344555746662" ID="ID_150596713" MODIFIED="1346969278674" TEXT="Network - population - set of RCSTs">
<icon BUILTIN="button_cancel"/>
</node>
<node CREATED="1344555768749" ID="ID_198347220" MODIFIED="1344611096029" TEXT="Similar to &quot;Collect Inbound Stats&quot; on LinkMon">
<icon BUILTIN="button_cancel"/>
</node>
<node CREATED="1344561147555" ID="ID_1817300797" MODIFIED="1345675472592" TEXT="For now just upload file, split on LF, insert">
<icon BUILTIN="button_ok"/>
<node CREATED="1344561169165" ID="ID_1591469838" MODIFIED="1344561174403" TEXT="Delete file when done"/>
</node>
<node CREATED="1345675456250" ID="ID_235768868" MODIFIED="1346969304179" TEXT="Allow deleting RCST list">
<icon BUILTIN="button_cancel"/>
</node>
<node CREATED="1344557127591" ID="ID_586543567" MODIFIED="1344611066696" TEXT="New tBatch table">
<icon BUILTIN="button_ok"/>
<node CREATED="1344557261360" ID="ID_503725959" MODIFIED="1344557267471" TEXT="iIsolationBatchId"/>
<node CREATED="1344557301197" ID="ID_286275757" MODIFIED="1344557327555" TEXT="oStartTime"/>
<node CREATED="1344557328996" ID="ID_365492120" MODIFIED="1344557341427" TEXT="oCompletedTime"/>
<node CREATED="1344557355962" ID="ID_1101781545" MODIFIED="1344557385248" TEXT="zStatus"/>
</node>
<node CREATED="1344557170781" ID="ID_337509541" MODIFIED="1344611081037" TEXT="New tBatchDetail table">
<icon BUILTIN="button_ok"/>
<node CREATED="1344557553367" ID="ID_1646700879" MODIFIED="1344557562894" TEXT="zRcstServerId">
<node CREATED="1344563580521" ID="ID_171387777" MODIFIED="1344563588923" TEXT="used termID instead"/>
</node>
<node CREATED="1336685362209" ID="ID_569293797" MODIFIED="1344557709404" TEXT="oCheckTime"/>
<node CREATED="1336685370873" ID="ID_1050619475" MODIFIED="1344557730372" TEXT="fCopolPeak"/>
<node CREATED="1336685396415" ID="ID_607214743" MODIFIED="1344557735612" TEXT="fXpolPeak"/>
<node CREATED="1336685404831" ID="ID_106065491" MODIFIED="1344557743620" TEXT="fIsolationValue"/>
<node CREATED="1344556893381" ID="ID_1523647659" MODIFIED="1344557034965" TEXT="iIsolationBatchId"/>
<node CREATED="1344557512001" ID="ID_1032683735" MODIFIED="1344557520624" TEXT="zResult"/>
</node>
<node CREATED="1345131757959" ID="ID_1054112039" MODIFIED="1345596798126" TEXT="New tNetwork">
<icon BUILTIN="button_ok"/>
<node CREATED="1345131826010" ID="ID_1437863957" MODIFIED="1345131850518" TEXT="iNetworkId"/>
<node CREATED="1345131851171" ID="ID_1320791136" MODIFIED="1345131856007" TEXT="iServerId"/>
<node CREATED="1345131900845" ID="ID_1581898699" MODIFIED="1345131907336" TEXT="zNetworkName"/>
<node CREATED="1345131857628" ID="ID_1374821158" MODIFIED="1345131879327" TEXT="zGcuIPAddress"/>
</node>
</node>
<node CREATED="1344558236352" FOLDED="true" ID="ID_852411320" MODIFIED="1345596739578" TEXT="Make so can review batch lists">
<icon BUILTIN="button_ok"/>
<node CREATED="1344561185973" ID="ID_589847774" MODIFIED="1344561197500" TEXT="This can be done easily in HAL"/>
</node>
<node CREATED="1344561943528" FOLDED="true" ID="ID_1012163021" MODIFIED="1345596734276" TEXT="Allow abort">
<icon BUILTIN="button_ok"/>
<node CREATED="1345166875895" ID="ID_1648736642" MODIFIED="1345596690533" TEXT="Make so so trigger only allows status change under certain circumstances">
<icon BUILTIN="button_ok"/>
<node CREATED="1345166997935" ID="ID_1264850919" MODIFIED="1345167003065" TEXT="Valid values">
<node CREATED="1345167004486" ID="ID_798456774" MODIFIED="1345167005875" TEXT="([zStatus]=&apos;Cancelled&apos; OR [zStatus]=&apos;Cancelling&apos; OR [zStatus]=&apos;CompletedSuccessfully&apos; OR [zStatus]=&apos;Failed&apos; OR [zStatus]=&apos;InProgress&apos; OR [zStatus]=&apos;WaitingToStart&apos;)"/>
</node>
<node CREATED="1345166936642" ID="ID_484741107" MODIFIED="1345562760425" TEXT="Is WaitingToStart">
<node CREATED="1345167032277" ID="ID_752989256" MODIFIED="1345167040831" TEXT="Changed to Cancelled"/>
<node CREATED="1345167042508" ID="ID_793370894" MODIFIED="1345167059007" TEXT="Changed to InProgress"/>
</node>
<node CREATED="1345167077730" ID="ID_215053848" MODIFIED="1345167103524" TEXT="Is Cancelling">
<node CREATED="1345167107352" ID="ID_952070408" MODIFIED="1345167114148" TEXT="Changed to Cancelled"/>
</node>
<node CREATED="1345167135455" ID="ID_1460667692" MODIFIED="1345167171017" TEXT="Is InProgress">
<node CREATED="1345167172397" ID="ID_1517855573" MODIFIED="1345167182264" TEXT="Changed to Failed"/>
<node CREATED="1345167184964" ID="ID_1825223844" MODIFIED="1345167193503" TEXT="Changed to CompletedSuccessfully"/>
<node CREATED="1345562710018" ID="ID_30166546" MODIFIED="1345562714959" TEXT="Changed to Cancelling"/>
</node>
<node CREATED="1345596693150" ID="ID_1544642419" MODIFIED="1345596723307" TEXT="Made so updates oUpdatedOn and checks that for change in UI too"/>
</node>
</node>
<node CREATED="1344562387262" ID="ID_693693003" MODIFIED="1345596816237" TEXT="Have operator input text &quot;Approved&quot;">
<icon BUILTIN="button_cancel"/>
</node>
<node CREATED="1344562447000" ID="ID_1813729003" MODIFIED="1345596819669" TEXT="Require start time to be in the future">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1344562473305" ID="ID_1872450500" MODIFIED="1345596824133" TEXT="Schedule GMT, but show local time">
<icon BUILTIN="button_cancel"/>
<node CREATED="1345596828103" ID="ID_802408128" MODIFIED="1345596866745" TEXT="Showing current UTC time."/>
</node>
</node>
<node CREATED="1344558882283" ID="ID_1549576304" MODIFIED="1346969299874" TEXT="Could import last isolation check data into FileMaker 5.5">
<icon BUILTIN="button_cancel"/>
</node>
</node>
<node CREATED="1344558548022" FOLDED="true" ID="ID_944179694" MODIFIED="1346969372446" TEXT="VsatXpolBatchMP">
<icon BUILTIN="button_ok"/>
<node CREATED="1344558458043" ID="ID_237984588" MODIFIED="1346969325884" TEXT="Is Windows service">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1344558466675" ID="ID_727734623" MODIFIED="1346969328780" TEXT="Looks for new batches">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1344558520104" FOLDED="true" ID="ID_369924342" MODIFIED="1346969352752" TEXT="External">
<icon BUILTIN="button_ok"/>
<node CREATED="1345674245211" ID="ID_14421854" MODIFIED="1346969334036" TEXT="Gets power level similar to linkmon">
<icon BUILTIN="button_cancel"/>
<node CREATED="1345674260295" ID="ID_1383339413" MODIFIED="1345674291689" TEXT="See &quot;Automated isolation check s/w&quot; email from David"/>
</node>
<node CREATED="1343324660644" ID="ID_1193630603" MODIFIED="1346969342116" TEXT="Put RCST into receive only mode and initiate CW">
<icon BUILTIN="button_ok"/>
<node CREATED="1344558966270" ID="ID_1213011473" MODIFIED="1344558980967" TEXT="Need class for talking to RNCC">
<node CREATED="1345739511526" ID="ID_1750515423" MODIFIED="1345739515738" TEXT="Old Components">
<node CREATED="1345739480237" ID="ID_314255809" MODIFIED="1345739508573" TEXT="TelnetRNCC.Java Class">
<node CREATED="1345739728538" ID="ID_1470792864" MODIFIED="1345739740934" TEXT="Connects to Linkstar DB to get RNCC info"/>
<node CREATED="1345739741353" ID="ID_1324550613" MODIFIED="1345739765927" TEXT="Telnets to RNCC and sends commands that go to the RCSTs"/>
</node>
<node CREATED="1345739491304" ID="ID_94434627" MODIFIED="1345739499291" TEXT="TelnetClient in C++">
<node CREATED="1345739526406" ID="ID_1489658700" MODIFIED="1345739554105" TEXT="Had an input buffer that could be flushed"/>
<node CREATED="1345739538357" ID="ID_296299798" MODIFIED="1345739714233" TEXT="Could wait for certain text to show up in the input buffer"/>
<node CREATED="1345739718123" ID="ID_1153155598" MODIFIED="1345739723735" TEXT="Able to timeout"/>
</node>
</node>
<node CREATED="1345739783111" ID="ID_1860976894" MODIFIED="1345739789269" TEXT="New Components">
<node CREATED="1345739790838" ID="ID_1594804629" MODIFIED="1346100513042" TEXT="C# MinimalisticTelnet class from codeproject.com">
<icon BUILTIN="button_ok"/>
<node CREATED="1345740394140" ID="ID_611238590" MODIFIED="1345740420649" TEXT="Hack up to work like C++ TelnetClient Kenyon created"/>
<node CREATED="1345740427597" ID="ID_1379554012" MODIFIED="1345740441874" TEXT="Include in MainstreamData.Utility or MainstreamData.Web">
<node CREATED="1345841895689" ID="ID_777444862" MODIFIED="1345841904406" TEXT="Decided on Web"/>
</node>
</node>
<node CREATED="1345740449887" ID="ID_1721512750" MODIFIED="1345740481100" TEXT="MainstreamData.Monitoring.Linkstar">
<node CREATED="1345740482496" ID="ID_1842940410" MODIFIED="1345841057305" TEXT="Create C# version of TelnetRncc class">
<node CREATED="1345841038516" ID="ID_404705476" MODIFIED="1345841075570" TEXT="Create HAL page similar to linkmon to test it"/>
</node>
</node>
</node>
</node>
</node>
<node CREATED="1343324727352" ID="ID_921100956" MODIFIED="1346969346973" TEXT="Retrieve signal data from RMP">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1336685338259" ID="ID_1985429243" MODIFIED="1346969349077" TEXT="Store data in DB">
<arrowlink DESTINATION="ID_337509541" ENDARROW="Default" ENDINCLINATION="368;0;" ID="Arrow_ID_1798649983" STARTARROW="None" STARTINCLINATION="368;0;"/>
<icon BUILTIN="button_ok"/>
</node>
</node>
<node CREATED="1344560352335" ID="ID_1340439713" MODIFIED="1346969357069" TEXT="Abort batch if too many errors">
<icon BUILTIN="button_ok"/>
</node>
</node>
<node CREATED="1336685415790" ID="ID_1955545144" MODIFIED="1336685435237" TEXT="Add alerts to HAL for any isolations out of spec"/>
<node CREATED="1336748498048" ID="ID_756882403" MODIFIED="1336748518058" TEXT="Should link some information back to the RCSTs already in HAL">
<node CREATED="1336748519231" ID="ID_454946629" MODIFIED="1336748528209" TEXT="Could do similar to Medias servers"/>
</node>
<node CREATED="1345131165045" ID="ID_1182008632" MODIFIED="1346970698375" TEXT="Documentation">
<node CREATED="1345131169942" ID="ID_644786878" MODIFIED="1346969394679" TEXT="Simply part of VsatXpol system (HAL Wiki)">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1345131187239" FOLDED="true" ID="ID_324449085" MODIFIED="1346970688159" TEXT="Add information about creating new HAL pages under MonitorPoint Framework">
<icon BUILTIN="button_ok"/>
<node CREATED="1345131210288" ID="ID_854490376" MODIFIED="1345131226709" TEXT="Copy VsatXpolBatchList and Status files">
<node CREATED="1345598058344" ID="ID_572627026" MODIFIED="1345598074429" TEXT="They inherit MonitorBasePage"/>
</node>
<node CREATED="1345131227137" ID="ID_766924" MODIFIED="1345131260310" TEXT="Update to match new monitor point">
<node CREATED="1345131294523" ID="ID_449721907" MODIFIED="1345131307944" TEXT="Can override setview, etc. as needed"/>
</node>
<node CREATED="1345131265962" ID="ID_481214152" MODIFIED="1345131283815" TEXT="Update summary and sitemap files"/>
<node CREATED="1345131284618" ID="ID_815693005" MODIFIED="1345131290136" TEXT="Update web.config"/>
<node CREATED="1345131320420" ID="ID_86872765" MODIFIED="1345131357641" TEXT="Update HAL DB as described in VsatXpol DB wiki"/>
</node>
<node CREATED="1345823593611" ID="ID_778431367" MODIFIED="1345823600928" TEXT="Release">
<node CREATED="1345823602260" ID="ID_1265741087" MODIFIED="1345823685651" TEXT="HAL 2.27">
<node CREATED="1345823610115" ID="ID_661057328" MODIFIED="1354228349149" TEXT="Separate from VsatXpol">
<icon BUILTIN="button_ok"/>
</node>
</node>
<node CREATED="1345823619788" ID="ID_931066978" MODIFIED="1345823690186" TEXT="VsatXpol 1.0.1">
<node CREATED="1345823660277" FOLDED="true" ID="ID_213491752" MODIFIED="1354227702306" TEXT="Also MainstreamData.Utility 1.0.6">
<node CREATED="1345841928074" ID="ID_1712010865" MODIFIED="1354227697561" TEXT="Release to include stringbuilder.clear method"/>
</node>
<node CREATED="1348016361906" ID="ID_1828877210" MODIFIED="1348016373317" TEXT="Also MainstreamData.Monitoring 1.0.7"/>
</node>
<node CREATED="1345823630204" ID="ID_1025687916" MODIFIED="1345823694739" TEXT="VsatXpolBatch 1.0.0">
<node CREATED="1345823645269" ID="ID_141190725" MODIFIED="1348172954729" TEXT="Also MainstreamData.Web 1.0.7 (for telnet)"/>
</node>
</node>
</node>
<node CREATED="1347657973238" FOLDED="true" ID="ID_1756822359" MODIFIED="1348180121138" TEXT="New features added after testing">
<icon BUILTIN="button_ok"/>
<node CREATED="1347657982667" ID="ID_785967303" MODIFIED="1348070681830" TEXT="&quot;Start Now&quot; button">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1348150472121" ID="ID_609458015" MODIFIED="1348176136263" TEXT="Order by batch id desc">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1347658303001" ID="ID_987354515" MODIFIED="1348179974403" TEXT="Highlight &quot;not found&quot; copol value">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1347662710126" ID="ID_1356945842" MODIFIED="1348179974404" TEXT="Zero isolation happens if copol not found or xpol peak higher than copol - flag in HAL">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1348093641715" ID="ID_484739951" MODIFIED="1348179974404" TEXT="Elapsed time not working correctly">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1348168155035" ID="ID_1591418417" MODIFIED="1348179974404" TEXT="Hover over RCST shows telnet results">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1348168169642" ID="ID_319271270" MODIFIED="1348179974403" TEXT="Hover over status shows status comments">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1347657992971" ID="ID_38699000" MODIFIED="1348083651057" TEXT="Check for CW gone after each RCST">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1347658006066" ID="ID_1781735677" MODIFIED="1348083657384" TEXT="Wait for CW for two cycles, then try send cw again">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1347658121556" ID="ID_666927571" MODIFIED="1348083699275" TEXT="Review CW sleep commands and replace with look for response">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1348164310566" ID="ID_118343202" MODIFIED="1348168196126" TEXT="See what telnet is showing when stopping clearwave and enabling term">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1348073756013" ID="ID_831385418" MODIFIED="1348172755151" TEXT="Make sure BatchMP shutdown flags batch in progress properly.">
<icon BUILTIN="button_ok"/>
<node CREATED="1348168220063" ID="ID_93476567" MODIFIED="1348168226120" TEXT="Decided just to cancel"/>
</node>
<node CREATED="1348092844467" ID="ID_274228027" MODIFIED="1348151853524" TEXT="Fix so CW staying up actually cancels batch">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1347658888688" FOLDED="true" ID="ID_1123268772" MODIFIED="1348013463067" TEXT="See if can detect when Agilent remote is running and back off">
<icon BUILTIN="button_ok"/>
<node CREATED="1348008458950" ID="ID_459274992" MODIFIED="1348008492421" TEXT="Only slows down a bit and avoids crashing. Also closes OK without crashing."/>
</node>
<node CREATED="1347658940637" FOLDED="true" ID="ID_1432566744" MODIFIED="1348013462133" TEXT="Stop sweeping as soon as unhandled exception is encountered">
<icon BUILTIN="button_ok"/>
<node CREATED="1347658968747" ID="ID_730176193" MODIFIED="1347658985264" TEXT="Need to stop in other monitor points as well">
<icon BUILTIN="yes"/>
</node>
<node CREATED="1348008557642" ID="ID_554828495" MODIFIED="1348008597745" TEXT="This is really only applicable when running in application mode and the OK box is blocking the app from closing."/>
</node>
<node CREATED="1347662023752" ID="ID_1779763657" MODIFIED="1348015914844" TEXT="Widened xpol to 25 khz since missed on one RCST that showed 35 iso but was really 27">
<icon BUILTIN="button_ok"/>
</node>
</node>
</node>
</node>
<node CREATED="1336748795231" ID="ID_729682713" MODIFIED="1347312744456" POSITION="left" TEXT="VsatXpol Design (no batch related info)">
<node CREATED="1336677501547" FOLDED="true" ID="ID_1885320177" MODIFIED="1343324876022" TEXT="Technology">
<icon BUILTIN="button_ok"/>
<node CREATED="1336677507395" ID="ID_1611900389" MODIFIED="1336677510938" TEXT="ASP.Net"/>
<node CREATED="1336677511419" ID="ID_549679928" MODIFIED="1336677513194" TEXT="C#"/>
<node CREATED="1336677514275" ID="ID_1764222705" MODIFIED="1336677777184" TEXT="WCF and the NetNamedPipeBinding for interprocess communication">
<node CREATED="1339614522049" ID="ID_234260094" MODIFIED="1339614532650" TEXT="Ended up using http/tcp"/>
</node>
</node>
<node CREATED="1336748805142" FOLDED="true" ID="ID_324273489" MODIFIED="1347312736960" TEXT="Components">
<icon BUILTIN="button_ok"/>
<node CREATED="1336754413392" FOLDED="true" ID="ID_526811411" MODIFIED="1347312729002" TEXT="Windows service on VSAT3RMN (VsatXpolRmp)">
<icon BUILTIN="button_ok"/>
<node CREATED="1336754428927" ID="ID_570851760" MODIFIED="1337874151739" TEXT="Exposes interface using WCF">
<arrowlink DESTINATION="ID_958427550" ENDARROW="Default" ENDINCLINATION="1447;0;" ID="Arrow_ID_545930014" STARTARROW="None" STARTINCLINATION="1447;0;"/>
<icon BUILTIN="button_ok"/>
<node CREATED="1336757515479" ID="ID_814317777" MODIFIED="1336757536881" TEXT="Methods">
<node CREATED="1336757538381" ID="ID_1512101286" MODIFIED="1342107648873" TEXT="get_SignalPairList (formerly GetCWStatusList)">
<icon BUILTIN="button_ok"/>
<node CREATED="1336757609497" ID="ID_64296246" MODIFIED="1336757626237" TEXT="Returns SignalData object for all CWs"/>
</node>
<node CREATED="1336757553477" ID="ID_755854244" MODIFIED="1337721964281" TEXT="ChangeCWState(cw)">
<icon BUILTIN="button_cancel"/>
<node CREATED="1337721966131" ID="ID_466323982" MODIFIED="1337721983401" TEXT="Decided this wasn&apos;t really necessary since happens automatically"/>
<node CREATED="1337721996820" ID="ID_1489182590" MODIFIED="1337722008033" TEXT="Can call initialize to reset everything"/>
</node>
<node CREATED="1336757639856" ID="ID_671884724" MODIFIED="1342107743804" TEXT="get_SignalPairStatus(slot) (GetCWStatus)">
<icon BUILTIN="button_ok"/>
<node CREATED="1336757661334" ID="ID_135129937" MODIFIED="1336757674690" TEXT="Returns SignalData object for single CW"/>
<node CREATED="1336757695884" ID="ID_18888603" MODIFIED="1336757728783" TEXT="As long as this has been called within the last minute, then keep sweeping CW"/>
<node CREATED="1342107754998" ID="ID_1741444346" MODIFIED="1342107771890" TEXT="Slot zero returns beacon information"/>
</node>
<node CREATED="1337722013445" ID="ID_99899264" MODIFIED="1342107789649" TEXT="Initialize(signalPairList)">
<icon BUILTIN="button_ok"/>
<node CREATED="1337722043486" ID="ID_17221373" MODIFIED="1337722064108" TEXT="Sets everything up and starts sweeping signals"/>
</node>
</node>
</node>
<node CREATED="1337637316503" ID="ID_1843384178" MODIFIED="1337874148467" TEXT="New/updated classes to handle multiple CWs">
<icon BUILTIN="button_ok"/>
<node CREATED="1337637322768" ID="ID_1331670180" MODIFIED="1337874106538" TEXT="Signal (formerly SignalData struct)">
<icon BUILTIN="button_ok"/>
<node CREATED="1337637337944" ID="ID_1318149037" MODIFIED="1337637756790" TEXT="Has all of the info about a signal and can calculate amplitude"/>
<node CREATED="1337637777323" ID="ID_1119802959" MODIFIED="1337637785976" TEXT="Stores state info about the signal">
<node CREATED="1337638331595" ID="ID_509410528" MODIFIED="1337638345104" TEXT="Has a type so can handle beacons, copol, and xpol"/>
</node>
<node CREATED="1337638779110" ID="ID_115046394" MODIFIED="1337638863078" TEXT="Tracks whenever frequencies change">
<node CREATED="1337638843081" ID="ID_140492112" MODIFIED="1337638850646" TEXT="Resets flag when read"/>
</node>
</node>
<node CREATED="1337637529344" ID="ID_389383743" MODIFIED="1342107608436" TEXT="SignalPair (formerly ClearWaveSignal) (new)">
<icon BUILTIN="button_ok"/>
<node CREATED="1337637535841" ID="ID_674637436" MODIFIED="1337637547989" TEXT="Stores a copol and xpol signal"/>
<node CREATED="1337637564290" ID="ID_1908349052" MODIFIED="1337637572144" TEXT="Can return isolation values"/>
<node CREATED="1342107802443" ID="ID_787507983" MODIFIED="1342107812162" TEXT="Can handle beacon pairs"/>
</node>
<node CREATED="1337637364138" ID="ID_896392238" MODIFIED="1337874131882" TEXT="SpecAnalyzer (updated)">
<icon BUILTIN="button_ok"/>
<node CREATED="1337637369929" ID="ID_817065998" MODIFIED="1337637997204" TEXT="Updates data in one or more SignalAnalyzer"/>
<node CREATED="1337637890096" ID="ID_511605826" MODIFIED="1337637921102" TEXT="SignalAnalyzers can be queued up for sweeping"/>
</node>
<node CREATED="1337637469478" ID="ID_105585192" MODIFIED="1337874142714" TEXT="IsolationAnalyzer (formerly XpolAnalyzer)">
<icon BUILTIN="button_ok"/>
<node CREATED="1337637480527" ID="ID_865244954" MODIFIED="1337637851643" TEXT="Interfaces two SpecAnalyzers to populate one or more XpolSignalAnalyzers"/>
<node CREATED="1337696373305" ID="ID_86053856" MODIFIED="1337696408496" TEXT="Accesses beacon signal data and XpolSignalAnalyzers via the SignalContainer"/>
</node>
<node CREATED="1337638685546" FOLDED="true" ID="ID_1525110345" MODIFIED="1337706830795" TEXT="SignalContainer">
<icon BUILTIN="button_cancel"/>
<node CREATED="1337638709275" ID="ID_1607650491" MODIFIED="1337638757673" TEXT="Can hold beacon SignalAnalyzers and mutiple XpolSignalAnalyzers"/>
<node CREATED="1337639181752" ID="ID_1550533987" MODIFIED="1337639193452" TEXT="Sets a flag when beacons change">
<node CREATED="1337639195768" ID="ID_1103374871" MODIFIED="1337639199068" TEXT="Clears when read"/>
</node>
<node CREATED="1337639200192" ID="ID_841333995" MODIFIED="1337639219885" TEXT="Sets a flag when XpolSignalAnalyzers change">
<node CREATED="1337639221705" ID="ID_85817327" MODIFIED="1337639224894" TEXT="Clears when read"/>
</node>
<node CREATED="1337706757087" ID="ID_1300235639" MODIFIED="1337706829177" TEXT="Use Initialize method of VsatXpolRmp instead">
<icon BUILTIN="button_ok"/>
</node>
</node>
</node>
</node>
<node CREATED="1336754423567" FOLDED="true" ID="ID_1551451827" MODIFIED="1337874179109" TEXT="Web service on VSAT3RMN (VsatXpolRmpWebService)">
<arrowlink DESTINATION="ID_626681909" ENDARROW="Default" ENDINCLINATION="399;0;" ID="Arrow_ID_495241384" STARTARROW="None" STARTINCLINATION="399;0;"/>
<icon BUILTIN="button_cancel"/>
<node CREATED="1336754441238" ID="ID_493491077" MODIFIED="1336754773438" TEXT="Uses WCF to expose same functionality as Windows service"/>
</node>
<node CREATED="1336754780091" ID="ID_330968176" MODIFIED="1343324539249" TEXT="ASP.Net on HAL (Monitor Point: VSAT3 Xpol)">
<icon BUILTIN="button_ok"/>
<node CREATED="1336754789347" ID="ID_395392342" MODIFIED="1336754896482" TEXT="Main page">
<node CREATED="1336754815529" ID="ID_87398461" MODIFIED="1336754877489" TEXT="Calls web service to get status of CWs"/>
<node CREATED="1336751734025" ID="ID_1633637728" MODIFIED="1337873983637" TEXT="Lists all CW slots and status"/>
</node>
<node CREATED="1336754888565" ID="ID_1815451316" MODIFIED="1336754892960" TEXT="Detail page">
<node CREATED="1336754898572" ID="ID_998097245" MODIFIED="1336757324806" TEXT="Calls web service to show current CW data"/>
</node>
<node CREATED="1336759926381" FOLDED="true" ID="ID_216674911" MODIFIED="1343324531938" TEXT="Finished ideas">
<node CREATED="1336750561256" ID="ID_412482873" MODIFIED="1336759818362" TEXT="Consider using .Net 2.0 and integrating with HAL from the start">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1336749094222" ID="ID_70273024" MODIFIED="1336759889725" TEXT="Initially, don&apos;t protect via login (will do later with HAL)">
<icon BUILTIN="button_cancel"/>
</node>
<node CREATED="1336748814646" ID="ID_1407509043" MODIFIED="1336759762101" TEXT="WebService for AJAX?">
<icon BUILTIN="button_cancel"/>
<node CREATED="1336759763678" ID="ID_137166633" MODIFIED="1336759791929" TEXT="Use ASP.Net UpdatePanel with asp:timer instead"/>
</node>
</node>
</node>
</node>
<node CREATED="1336748896097" FOLDED="true" ID="ID_821534919" MODIFIED="1344556656082" TEXT="Development Steps">
<icon BUILTIN="button_ok"/>
<node CREATED="1336749078831" ID="ID_1319436555" MODIFIED="1336767466796" TEXT="Consider what web pages will be needed">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1336748905537" FOLDED="true" ID="ID_16426417" MODIFIED="1342107211708" TEXT="Fix problems with current software">
<arrowlink DESTINATION="ID_1217136789" ENDARROW="Default" ENDINCLINATION="188;0;" ID="Arrow_ID_1372048300" STARTARROW="None" STARTINCLINATION="188;0;"/>
<icon BUILTIN="button_ok"/>
<node CREATED="1337889563961" ID="ID_1160321141" MODIFIED="1337889572549" TEXT="Offset">
<node CREATED="1337889612596" ID="ID_1642387423" MODIFIED="1337889633215" TEXT="Assumes that the center is the frequency"/>
<node CREATED="1337890120969" ID="ID_985638296" MODIFIED="1337890135138" TEXT="Also consider when to include it"/>
</node>
<node CREATED="1337891029488" ID="ID_959958025" MODIFIED="1337891051883" TEXT="Frequency per sample">
<node CREATED="1337891053017" ID="ID_708340882" MODIFIED="1337891075597" TEXT="E.g. start at 0 and end at 400 = 401 samples"/>
</node>
</node>
<node CREATED="1336748945022" FOLDED="true" ID="ID_585302830" MODIFIED="1337874200382" TEXT="Create Windows service using the Monitor Point Framework">
<icon BUILTIN="button_ok"/>
<node CREATED="1336751706291" ID="ID_413275391" MODIFIED="1336751725084" TEXT="Consider what security the Windows service will need"/>
<node CREATED="1336767489077" ID="ID_433230916" MODIFIED="1336767504554" TEXT="Could use the same security as MediasWebsiteLmp"/>
</node>
<node CREATED="1336767516187" ID="ID_104370546" MODIFIED="1337873996277" TEXT="Add Xpol non-UI code to the new service">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1336748986964" ID="ID_732799538" MODIFIED="1337873999589" TEXT="Add classes that support WCF to the new service">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1336767564040" FOLDED="true" ID="ID_1902046130" MODIFIED="1337874203710" TEXT="Create web service that exposes same methods as Windows service">
<icon BUILTIN="button_cancel"/>
<node CREATED="1337874007807" ID="ID_626681909" MODIFIED="1337874176813" TEXT="Can just use WCF via http on a different port"/>
</node>
<node CREATED="1337880833763" ID="ID_1495749305" MODIFIED="1343324489452" TEXT="Test using old UI with single CW, then multiple CWs">
<icon BUILTIN="button_ok"/>
<node CREATED="1342105693406" ID="ID_1524693872" MODIFIED="1342105710174" TEXT="Did it with single UI only so far"/>
</node>
<node CREATED="1337880853892" ID="ID_667113288" MODIFIED="1337882580788" TEXT="Make spec analyzer address and gpib port settings be available in app.config">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1341332500556" FOLDED="true" ID="ID_1182350663" MODIFIED="1343097120724" TEXT="Improve Design">
<icon BUILTIN="button_ok"/>
<node CREATED="1341332567037" ID="ID_282740193" MODIFIED="1342105642800" TEXT="Use IList and List instead of arrays where sensible">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1341347934708" ID="ID_1023503482" MODIFIED="1342105652044" TEXT="Fix new FxCop warnings">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1341347957428" ID="ID_1684474761" MODIFIED="1342105660356" TEXT="Pass beacons as signalPairList[0] between LMP and RMP">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1342631544129" ID="ID_1842221892" MODIFIED="1343092607015" TEXT="Widescan vs Narrow (Offset problem)">
<icon BUILTIN="button_ok"/>
<node CREATED="1342631558201" ID="ID_66370817" MODIFIED="1342631574912" TEXT="Right now it&apos;s not centering after widescan">
<node CREATED="1342631600011" ID="ID_601791961" MODIFIED="1342631645954" TEXT="Need to store offset, then do narrow scan on offset frequency"/>
<node CREATED="1342631653469" ID="ID_1264248607" MODIFIED="1342631666555" TEXT="Also, all future scans should be done on offset frequency"/>
</node>
</node>
</node>
<node CREATED="1336767612254" FOLDED="true" ID="ID_1234930801" MODIFIED="1343092629433" TEXT="Create HAL main page utilizing web service">
<icon BUILTIN="button_ok"/>
<node CREATED="1340743865829" FOLDED="true" ID="ID_1512940822" MODIFIED="1342105967755" TEXT="Have LMP">
<icon BUILTIN="button_ok"/>
<node CREATED="1340743873270" ID="ID_1527457292" MODIFIED="1342105962386" TEXT="Connects to RMP when instanciated">
<icon BUILTIN="button_cancel"/>
</node>
<node CREATED="1340744943763" ID="ID_295809171" MODIFIED="1342105955059" TEXT="provide">
<icon BUILTIN="button_ok"/>
<node CREATED="1340744950964" ID="ID_1026274587" MODIFIED="1340744959054" TEXT="SatelliteName"/>
<node CREATED="1340744959628" ID="ID_1596525616" MODIFIED="1340744968664" TEXT="Networks"/>
<node CREATED="1340744969556" ID="ID_501261340" MODIFIED="1340744977542" TEXT="WcfPath"/>
</node>
<node CREATED="1340743909887" ID="ID_862081211" MODIFIED="1340744796307" TEXT="Times out after a couple of minutes and closes connections">
<icon BUILTIN="button_cancel"/>
</node>
<node CREATED="1340744800757" ID="ID_184500086" MODIFIED="1342105923037" TEXT="Provides same methods as RMP">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1340744908450" ID="ID_375449834" MODIFIED="1342105937461" TEXT="Web.config could store RMP host data">
<icon BUILTIN="button_cancel"/>
</node>
</node>
<node CREATED="1340990687474" ID="ID_1071354779" MODIFIED="1342107521107" TEXT="CW List in DB">
<node CREATED="1340990791467" ID="ID_1949899947" MODIFIED="1342105879583" TEXT="reqHalStatus return satellites with 0 up and 0 down">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1340990839024" ID="ID_1269233180" MODIFIED="1342105885367" TEXT="selServerByStatus return satellite and cw info">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1340990894821" ID="ID_1783456472" MODIFIED="1342105991489" TEXT="VsatXpolList.asmx attempts initialize on each refresh">
<icon BUILTIN="button_ok"/>
<node CREATED="1340990921444" ID="ID_1569654633" MODIFIED="1342105983633" TEXT="RMP only initializes if values change or force is set to true">
<icon BUILTIN="button_ok"/>
</node>
</node>
<node CREATED="1340991070115" FOLDED="true" ID="ID_1391467206" MODIFIED="1342201505182" TEXT="Procedure to refresh list">
<icon BUILTIN="button_ok"/>
<node CREATED="1340991051564" ID="ID_1438904971" MODIFIED="1342105997840" TEXT="Retrieve list of RMPs and signal frequencies from DB">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1341348068745" ID="ID_574983443" MODIFIED="1342106062741" TEXT="Go through frequencies and add them to list">
<icon BUILTIN="button_cancel"/>
<node CREATED="1342106065086" ID="ID_888268105" MODIFIED="1342106076490" TEXT="This is done automatically since comes from DB"/>
</node>
<node CREATED="1342106092181" ID="ID_1276330856" MODIFIED="1342201443039" TEXT="Build list of signals for each RMP">
<icon BUILTIN="button_cancel"/>
<node CREATED="1342108376762" ID="ID_1466419872" MODIFIED="1342108392058" TEXT="If no beacons then error out">
<node CREATED="1342108543433" ID="ID_755896331" MODIFIED="1342108568670" TEXT="Show down for all entries with &quot;Beacons not specified&quot;"/>
</node>
<node CREATED="1342201420699" ID="ID_167814004" MODIFIED="1342201438412" TEXT="This is handled below"/>
</node>
<node CREATED="1342106134362" ID="ID_625171527" MODIFIED="1342201446320" TEXT="Initialize each RMP via LMP">
<icon BUILTIN="button_ok"/>
<node CREATED="1342116002870" ID="ID_1937253771" MODIFIED="1342116004775" TEXT="steps">
<node CREATED="1342116005942" ID="ID_997631544" MODIFIED="1342116106527" TEXT="Get LMP reference"/>
<node CREATED="1342116115760" ID="ID_453014776" MODIFIED="1342116128918" TEXT="Store errors and state"/>
<node CREATED="1342116033564" ID="ID_1190416972" MODIFIED="1342116097951" TEXT="Build SignalPair list"/>
<node CREATED="1342116071538" ID="ID_452478515" MODIFIED="1342201472437" TEXT="Initialize LMP"/>
<node CREATED="1342116085258" ID="ID_1833317558" MODIFIED="1342116171707" TEXT="Update status for &quot;up&quot; signals"/>
</node>
</node>
<node CREATED="1340991087618" ID="ID_736904097" MODIFIED="1342201485010" TEXT="Get CW status list from RMP">
<icon BUILTIN="button_ok"/>
</node>
<node CREATED="1340991105473" ID="ID_716222022" MODIFIED="1342201498763" TEXT="Go through grid rows (acutally underlying table) and update data for each signal">
<icon BUILTIN="button_ok"/>
</node>
</node>
</node>
<node CREATED="1342108210556" ID="ID_1572269901" MODIFIED="1342201479633" TEXT="Having hard time coming to grips with reconciling list from DB against data from RMPs">
<icon BUILTIN="button_ok"/>
<node CREATED="1342109734388" ID="ID_1757285419" MODIFIED="1342201472438" TEXT="Not sure what to do on change of server">
<arrowlink DESTINATION="ID_452478515" ENDARROW="Default" ENDINCLINATION="193;0;" ID="Arrow_ID_1183145700" STARTARROW="None" STARTINCLINATION="193;0;"/>
</node>
</node>
</node>
<node CREATED="1336767675914" FOLDED="true" ID="ID_668938770" MODIFIED="1343324451199" TEXT="Create HAL detail page utilizing web service and Xpol UI code">
<icon BUILTIN="button_ok"/>
<node CREATED="1342202124967" ID="ID_711442732" MODIFIED="1342202148718" TEXT="See &quot;Xpol Followup Jun 2012&quot; Outlook post item for steps"/>
<node CREATED="1342631672319" ID="ID_1250776672" MODIFIED="1343092584846" TEXT="HAL not getting data while test UI does">
<icon BUILTIN="button_ok"/>
<node CREATED="1342631886144" ID="ID_483004337" MODIFIED="1343092527757" TEXT="Must be the LMP">
<icon BUILTIN="button_cancel"/>
<node CREATED="1342637853348" ID="ID_1825743828" MODIFIED="1343092562085" TEXT="Added code to test UI and VsatXpolLmpList to test"/>
<node CREATED="1342637904720" ID="ID_1478104490" MODIFIED="1342637928521" TEXT="Appears that the http session variable may be dropping the data">
<node CREATED="1342639162668" ID="ID_1463495203" MODIFIED="1342639185140" TEXT="This is probably due to the fact that it serializes/deserializes the data"/>
</node>
<node CREATED="1342639382487" ID="ID_1629927891" MODIFIED="1342639384441" TEXT="Options">
<node CREATED="1342639385311" ID="ID_1067412742" MODIFIED="1342639407526" TEXT="Try making objects (especially Signal) serializable">
<node CREATED="1342648378321" ID="ID_1368740333" MODIFIED="1342648459015" TEXT="Got data back to HAL by running UI client and HAL at the same time, so that rules out a serializable problem - more likely is the initialization is doing something wrong from HAL"/>
</node>
<node CREATED="1342639920121" ID="ID_1638191783" MODIFIED="1342641541276" TEXT="Try creating LMP on Status web page">
<icon BUILTIN="button_cancel"/>
<node CREATED="1342641528437" ID="ID_1624543356" MODIFIED="1342641536908" TEXT="Didn&apos;t appear to make any difference"/>
</node>
<node CREATED="1342639410318" ID="ID_253223976" MODIFIED="1342639497064" TEXT="Pass selServerByStatus stored procedure data to LMP and recreate VsatXpolLmp as needed"/>
</node>
</node>
<node CREATED="1343092529980" ID="ID_403962906" MODIFIED="1343092565181" TEXT="Was the Signal.CloneWithoutData method clearing the Data">
<icon BUILTIN="button_ok"/>
</node>
</node>
<node CREATED="1343092636714" ID="ID_498079866" MODIFIED="1343094997325" TEXT="Signals not being processed simultaneously">
<icon BUILTIN="button_ok"/>
<node CREATED="1343092665206" ID="ID_1318044586" MODIFIED="1343092700029" TEXT="Have to wait for one to go inactive before others come through"/>
<node CREATED="1343093229123" ID="ID_274243456" MODIFIED="1343094994909" TEXT="Was simple problem with SpecAnalyzer.IncrementSignalIndex not incrementing it.">
<icon BUILTIN="button_ok"/>
</node>
</node>
<node CREATED="1342648483417" ID="ID_1765892021" MODIFIED="1343097108448" TEXT="VsatXpolLmpList">
<icon BUILTIN="button_ok"/>
<node CREATED="1342648500866" ID="ID_95242396" MODIFIED="1342648554620" TEXT="Should use static (global) instance (instead of session) that can be refreshed by HAL as needed"/>
</node>
</node>
<node CREATED="1336767847872" FOLDED="true" ID="ID_1891704462" MODIFIED="1343324479069" TEXT="Configure HAL to show &quot;VSAT3 Xpol&quot; link under monitoring">
<icon BUILTIN="button_ok"/>
<node CREATED="1343324460299" ID="ID_1659007204" MODIFIED="1343324473821" TEXT="This is part of the release - Patrick will do."/>
</node>
</node>
<node CREATED="1336759689899" ID="ID_1194733142" MODIFIED="1343324393017" TEXT="Later">
<node CREATED="1337873955020" ID="ID_572456965" MODIFIED="1337873974390" TEXT="Consider adding security to windows service">
<arrowlink DESTINATION="ID_585302830" ENDARROW="Default" ENDINCLINATION="260;0;" ID="Arrow_ID_1922689388" STARTARROW="None" STARTINCLINATION="260;0;"/>
</node>
</node>
</node>
<node CREATED="1337889554729" ID="ID_1538510937" MODIFIED="1337889560692" POSITION="left" TEXT="Brainstorming"/>
</node>
</map>
