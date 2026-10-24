const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'..');
const read=file=>JSON.parse(fs.readFileSync(path.join(root,file),'utf8').replace(/^\uFEFF/,''));
const delivery=read('delivery.json'),technical=read('Review/technical-checks.json'),jobs=read('production-jobs.json');
const actors=delivery.actors.map(a=>({id:a.id,passed:a.review.passed,notes:a.review.notes,selectionRecord:`Review/${a.id}-selection.json`,clips:a.clips.map(c=>({motion:c.motion,frameCount:c.frames?.length||0,durationMs:c.frameDurationsMs?.reduce((s,n)=>s+n,0)||c.holdMs+c.fadeMs,contactFrame:c.contactFrame??null,contactTimeMs:c.contactFrame>=0?c.frameDurationsMs.slice(0,c.contactFrame).reduce((s,n)=>s+n,0):null,loop:c.loop??false}))}));
const report={version:3,status:actors.length===10?'complete':'production',actorCount:actors.length,selectedFrameCount:delivery.actors.reduce((s,a)=>s+a.clips.reduce((n,c)=>n+(c.frames?.length||0),0),0),
 visualReview:{method:'Full contact sheets and selected frames; browser playback sampled at 1x and 0.5x; paused attack contact poses and loop boundaries. This is sprite-level review, not Unity runtime QA.',actors},
 hitReview:{effect:'whiteFlash',holdMs:45,fadeMs:75,alpha:'preserved',geometry:'current pose, no generated Hit frames',browserEvidence:'gate_sentry attack pose and hook_hound attack contact pose were whitened and returned to the same colored pose; hook_hound additionally inspected at 0.25x.'},
 technicalReview:{passed:technical.passed&&technical.actorCount===actors.length,record:'Review/technical-checks.json',scope:technical.checked},
 production:{tool:'PixelLab',jobCount:jobs.length,rejectedJobs:jobs.filter(j=>j.status==='rejected').map(j=>({actor:j.actor,motion:j.motion,attempt:j.attempt,jobId:j.jobId,reason:j.rejectionReason||j.rejection||j.reason||j.visualReview})),record:'production-jobs.json'},
 integration:{manifest:'delivery.json',preview:'preview.html',overview:'overview.png',canvas:[128,128],pivotTopLeft:[78,114],referenceHeightPixels:92,staticBaseBetweenActions:true,providedStates:['Attack','Hit','Stun'],gameAppliedByThisTask:false}};
fs.writeFileSync(path.join(root,'Review','review-report.json'),JSON.stringify(report,null,2));
console.log(JSON.stringify({status:report.status,actors:report.actorCount,frames:report.selectedFrameCount,technicalPassed:report.technicalReview.passed,jobs:jobs.length,rejections:report.production.rejectedJobs.length}));
